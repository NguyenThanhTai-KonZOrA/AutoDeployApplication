using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientLauncher.Services
{
    public class DeploymentPollingService : IDeploymentPollingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IClientRegistrationService _registrationService;
        private readonly IInstallationService _installationService;
        private readonly IShortcutService _shortcutService;
        private readonly IIconService _iconService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DeploymentPollingService(
            IClientRegistrationService registrationService,
            IInstallationService installationService,
            IShortcutService shortcutService,
            IIconService iconService)
        {
            _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromMinutes(5)
            };
            _registrationService = registrationService;
            _installationService = installationService;
            _shortcutService = shortcutService;
            _iconService = iconService;
        }

        public async Task<List<DeploymentTaskDto>> GetPendingTasksAsync()
        {
            try
            {
                var machineId = _registrationService.GetMachineId();
                var response = await _httpClient.GetAsync($"/api/deploymenttask/pending/{machineId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<DeploymentTaskDto>>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        Logger.Info("Found {Count} pending deployment tasks", apiResponse.Data.Count);
                        return apiResponse.Data;
                    }
                }

                return new List<DeploymentTaskDto>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting pending tasks");
                return new List<DeploymentTaskDto>();
            }
        }

        public async Task ProcessPendingTasksAsync()
        {
            try
            {
                var tasks = await GetPendingTasksAsync();

                if (!tasks.Any())
                {
                    Logger.Debug("No pending tasks to process");
                    return;
                }

                Logger.Info("Processing {Count} pending tasks", tasks.Count);

                // Process tasks one by one (in priority order)
                foreach (var task in tasks.OrderByDescending(t => t.Priority))
                {
                    await ProcessSingleTaskAsync(task);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing pending tasks");
            }
        }

        private async Task ProcessSingleTaskAsync(DeploymentTaskDto task)
        {
            try
            {
                Logger.Info("Starting deployment task {TaskId}: {AppName} v{Version}",
                    task.Id, task.AppName, task.Version);

                // Update task status to InProgress
                await UpdateTaskStatusAsync(task.Id, "InProgress", 0, "Starting installation");


                var launcherPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

                // UPDATED: Use IconService to get file path for shortcut
                var iconPath = _iconService.GetIconFilePath(task.IconUrl, task.Category);

                // Create Desktop Icon
                var shortcutCreated = _shortcutService.CreateDesktopShortcut(
                           task.AppCode,
                           task.AppName,
                           launcherPath,
                           iconPath
                       );

                if (shortcutCreated)
                {
                    Logger.Info($"Successfully installed {task.AppName}");
                }
                else
                {
                    Logger.Error($"Failed to create shortcut for {task.AppName}");
                }

                // Perform installation
                var result = await _installationService.InstallApplicationAsync(
                    task.AppCode,
                    MachineInfoHelper.GetUserName());

                // Update task status based on result
                if (result.Success)
                {
                    await UpdateTaskStatusAsync(
                        task.Id,
                        "Completed",
                        100,
                        "Installation completed",
                        isSuccess: true);

                    Logger.Info("Task {TaskId} completed successfully", task.Id);
                }
                else
                {
                    await UpdateTaskStatusAsync(
                        task.Id,
                        "Failed",
                        0,
                        "Installation failed",
                        isSuccess: false,
                        errorMessage: result.Message);

                    Logger.Error("Task {TaskId} failed: {Error}", task.Id, result.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing task {TaskId}", task.Id);

                await UpdateTaskStatusAsync(
                    task.Id,
                    "Failed",
                    0,
                    "Exception occurred",
                    isSuccess: false,
                    errorMessage: ex.Message);
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(
            int taskId,
            string status,
            int progressPercentage,
            string? currentStep = null,
            bool isSuccess = false,
            string? errorMessage = null,
            long? downloadSizeBytes = null)
        {
            try
            {
                var updateRequest = new DeploymentTaskUpdateDto
                {
                    TaskId = taskId,
                    Status = status,
                    ProgressPercentage = progressPercentage,
                    CurrentStep = currentStep,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    DownloadSizeBytes = downloadSizeBytes
                };

                var json = JsonSerializer.Serialize(updateRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/deploymenttask/update-status", content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Debug("Task {TaskId} status updated to {Status}", taskId, status);
                    return true;
                }
                else
                {
                    Logger.Warn("Failed to update task status. Status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating task status");
                return false;
            }
        }

        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Message { get; set; }
        }
    }
}
