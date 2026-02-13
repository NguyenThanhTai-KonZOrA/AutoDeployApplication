using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ClientLauncherService;

public class DeploymentWorker : BackgroundService
{
    private readonly ILogger<DeploymentWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private string? _machineId;
    private readonly string _baseUrl;
    private readonly int _heartbeatIntervalSeconds = 30;
    private readonly int _pollingIntervalSeconds = 30;

    public DeploymentWorker(ILogger<DeploymentWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _baseUrl = configuration["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
        _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 ClientLauncher Service starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Initial registration
            await RegisterMachineAsync();

            using PeriodicTimer heartbeatTimer = new(TimeSpan.FromSeconds(_heartbeatIntervalSeconds));
            using PeriodicTimer pollingTimer = new(TimeSpan.FromSeconds(_pollingIntervalSeconds));

            // Start both timers
            var heartbeatTask = HeartbeatLoopAsync(heartbeatTimer, stoppingToken);
            var pollingTask = PollingLoopAsync(pollingTimer, stoppingToken);

            await Task.WhenAll(heartbeatTask, pollingTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error in DeploymentWorker");
            throw;
        }
    }

    private async Task HeartbeatLoopAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SendHeartbeatAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat loop");
            }
        }
    }

    private async Task PollingLoopAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PollAndProcessTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in polling loop");
            }
        }
    }

    private async Task RegisterMachineAsync()
    {
        try
        {
            _machineId = GetMachineId();
            _logger.LogInformation("🔧 Registering machine: {MachineId}", _machineId);

            var registerRequest = new
            {
                MachineId = _machineId,
                MachineName = Environment.MachineName,
                ComputerName = Environment.MachineName,
                UserName = Environment.UserName,
                DomainName = Environment.UserDomainName,
                IPAddress = GetIPAddress(),
                MACAddress = GetMACAddress(),
                OSVersion = Environment.OSVersion.ToString(),
                OSArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                CPUInfo = Environment.ProcessorCount + " Processors",
                TotalMemoryMB = 8192L,
                AvailableDiskSpaceGB = GetAvailableDiskSpace(),
                InstalledApplications = GetInstalledApplications(),
                ClientVersion = "1.0.0",
                Location = "Auto-detected (Service)"
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/clientmachine/register", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Machine registered successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to register machine: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error registering machine");
        }
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            var heartbeatRequest = new
            {
                MachineId = _machineId,
                Status = "Online",
                InstalledApplications = GetInstalledApplications(),
                AvailableDiskSpaceGB = GetAvailableDiskSpace()
            };

            var json = JsonSerializer.Serialize(heartbeatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/clientmachine/heartbeat", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("💓 Heartbeat sent successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending heartbeat");
        }
    }

    private async Task PollAndProcessTasksAsync()
    {
        try
        {
            _logger.LogDebug("🔍 Polling for deployment tasks...");

            var response = await _httpClient.GetAsync($"/api/deploymenttask/pending/{_machineId}");
            if (!response.IsSuccessStatusCode) return;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeploymentTaskDto>>>();
            if (result?.Success != true || result.Data == null || !result.Data.Any())
            {
                return;
            }

            _logger.LogInformation("📦 Found {Count} pending deployment tasks", result.Data.Count);

            foreach (var task in result.Data.OrderByDescending(t => t.Priority))
            {
                await ProcessTaskAsync(task);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error polling for tasks");
        }
    }

    private async Task ProcessTaskAsync(DeploymentTaskDto task)
    {
        try
        {
            _logger.LogInformation("🚀 Starting deployment task {TaskId}: {AppName} v{Version}",
                task.Id, task.AppName, task.Version);

            // Update status to InProgress
            await UpdateTaskStatusAsync(task.Id, "InProgress", 0, "Starting installation");

            // Call InstallApplication endpoint
            var installRequest = new { AppCode = task.AppCode, UserName = Environment.UserName };
            var json = JsonSerializer.Serialize(installRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/installation/install", content);

            if (response.IsSuccessStatusCode)
            {
                await UpdateTaskStatusAsync(task.Id, "Completed", 100, "Installation completed", true);
                _logger.LogInformation("✅ Task {TaskId} completed successfully", task.Id);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await UpdateTaskStatusAsync(task.Id, "Failed", 0, "Installation failed", false, error);
                _logger.LogError("❌ Task {TaskId} failed: {Error}", task.Id, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing task {TaskId}", task.Id);
            await UpdateTaskStatusAsync(task.Id, "Failed", 0, "Exception occurred", false, ex.Message);
        }
    }

    private async Task UpdateTaskStatusAsync(int taskId, string status, int progress, 
        string? currentStep = null, bool isSuccess = false, string? errorMessage = null)
    {
        try
        {
            var updateRequest = new
            {
                TaskId = taskId,
                Status = status,
                ProgressPercentage = progress,
                CurrentStep = currentStep,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            var json = JsonSerializer.Serialize(updateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/api/deploymenttask/update-status", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status");
        }
    }

    private string GetMachineId()
    {
        var combined = $"{Environment.MachineName}-{Environment.UserName}-{GetMACAddress()}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
    }

    private string GetIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "Unknown";
    }

    private string GetMACAddress()
    {
        try
        {
            var nic = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                    n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
            return nic?.GetPhysicalAddress().ToString() ?? "Unknown";
        }
        catch { }
        return "Unknown";
    }

    private long GetAvailableDiskSpace()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            long totalAvailable = drives.Sum(d => d.AvailableFreeSpace);
            return totalAvailable / (1024 * 1024 * 1024);
        }
        catch { }
        return 0;
    }

    private List<string> GetInstalledApplications()
    {
        var installedApps = new List<string>();
        try
        {
            var appsBasePath = _configuration["AppsBasePath"] ?? @"C:\CompanyApps";
            if (Directory.Exists(appsBasePath))
            {
                var appDirectories = Directory.GetDirectories(appsBasePath);
                foreach (var dir in appDirectories)
                {
                    var appCode = Path.GetFileName(dir);
                    if (appCode != "Icons" && appCode != "Temp" && !appCode.StartsWith("."))
                    {
                        var manifestPath = Path.Combine(dir, "App", "manifest.json");
                        if (File.Exists(manifestPath))
                        {
                            installedApps.Add(appCode);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installed applications");
        }
        return installedApps;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🛑 ClientLauncher Service stopping at: {time}", DateTimeOffset.Now);

        // Send final heartbeat to mark as Offline
        try
        {
            var offlineRequest = new
            {
                MachineId = _machineId,
                Status = "Offline"
            };
            var json = JsonSerializer.Serialize(offlineRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/api/clientmachine/heartbeat", content);
            _logger.LogInformation("✅ Marked machine as Offline");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking machine as offline");
        }

        await base.StopAsync(stoppingToken);
    }
}

// DTOs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class DeploymentTaskDto
{
    public int Id { get; set; }
    public string AppCode { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Priority { get; set; }
}
