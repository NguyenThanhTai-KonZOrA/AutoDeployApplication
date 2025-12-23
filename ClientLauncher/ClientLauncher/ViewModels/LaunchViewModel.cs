using ClientLauncher.Helpers;
using ClientLauncher.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ClientLauncher.ViewModels
{
    public class LaunchViewModel : ViewModelBase
    {
        private readonly string _appCode;
        private readonly Window _window;
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl = "https://localhost:7172";
        private readonly string _appsBasePath = @"C:\CompanyApps";

        private string _appName = "Loading...";
        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        private string _statusEmoji = "🔄";
        public string StatusEmoji
        {
            get => _statusEmoji;
            set => SetProperty(ref _statusEmoji, value);
        }

        private string _statusMessage = "Initializing...";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private bool _isProcessing = true;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private bool _isIndeterminate = true;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        private bool _canCancel = true;
        public bool CanCancel
        {
            get => _canCancel;
            set => SetProperty(ref _canCancel, value);
        }

        public RelayCommand CancelCommand { get; }

        public LaunchViewModel(string appCode, Window window)
        {
            _appCode = appCode;
            _window = window;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            CancelCommand = new RelayCommand(_ => Cancel());

            // Start the launch sequence
            _ = LaunchSequenceAsync();
        }

        private async Task LaunchSequenceAsync()
        {
            try
            {
                // Step 1: Get app info
                StatusMessage = "Loading application information...";
                await GetAppInfoAsync();

                await Task.Delay(500);

                // Step 2: Check for updates
                StatusEmoji = "🔍";
                StatusMessage = "Checking for updates...";
                ProgressValue = 20;

                var hasUpdate = await CheckForUpdatesAsync();

                if (hasUpdate)
                {
                    // Step 3: Download and apply updates
                    StatusEmoji = "⬇️";
                    StatusMessage = "Downloading updates...";
                    ProgressValue = 40;

                    await ApplyUpdatesAsync();

                    ProgressValue = 80;
                    await Task.Delay(300);
                }
                else
                {
                    ProgressValue = 60;
                }

                // Step 4: Launch application
                StatusEmoji = "🚀";
                StatusMessage = "Launching application...";
                ProgressValue = 90;

                await Task.Delay(500);

                LaunchApplication();

                ProgressValue = 100;
                StatusEmoji = "✅";
                StatusMessage = "Application launched successfully!";

                await Task.Delay(1000);

                // Close launcher window
                _window.Close();
            }
            catch (Exception ex)
            {
                StatusEmoji = "❌";
                StatusMessage = $"Error: {ex.Message}";
                IsProcessing = false;
                CanCancel = true;
            }
        }

        private async Task GetAppInfoAsync()
        {
            try
            {
                var url = $"{_serverUrl}/api/AppCatalog/applications/{_appCode}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiBaseResponse<ApplicationInfo>>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        AppName = apiResponse.Data.Name;
                    }
                }
            }
            catch
            {
                AppName = _appCode;
            }
        }

        private async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                // Get local version
                var versionFile = Path.Combine(_appsBasePath, _appCode, "App", "version.txt");
                var localVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "0.0.0";

                // Get server manifest
                var manifestUrl = $"{_serverUrl}/api/apps/{_appCode}/manifest";
                var response = await _httpClient.GetAsync(manifestUrl);

                if (!response.IsSuccessStatusCode)
                    return false;

                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiBaseResponse<ManifestInfo>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    var serverVersion = apiResponse.Data.Binary?.Version ?? "0.0.0";

                    // Compare versions
                    var server = new Version(serverVersion);
                    var local = new Version(localVersion);

                    return server > local;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task ApplyUpdatesAsync()
        {
            // Call Installation Service to update
            var apiService = new ApiService();
            var userName = Environment.UserName;

            // In a real scenario, you'd call UpdateApplicationAsync
            // For now, we just simulate
            await Task.Delay(2000);
        }

        private void LaunchApplication()
        {
            try
            {
                var appPath = Path.Combine(_appsBasePath, _appCode, "App");

                // Find the main executable (assume first .exe file)
                var exeFiles = Directory.GetFiles(appPath, "*.exe", SearchOption.TopDirectoryOnly);

                if (exeFiles.Length > 0)
                {
                    var exePath = exeFiles[0];

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = appPath,
                        UseShellExecute = true
                    };

                    Process.Start(processInfo);
                }
                else
                {
                    throw new Exception($"No executable found in {appPath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch application: {ex.Message}", ex);
            }
        }

        private void Cancel()
        {
            _window.Close();
        }
    }

    // Helper classes
    public class ApplicationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;
    }

    public class ManifestInfo
    {
        public BinaryVersionInfo? Binary { get; set; }
    }

    public class BinaryVersionInfo
    {
        public string Version { get; set; } = string.Empty;
    }

    public class ApiBaseResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}