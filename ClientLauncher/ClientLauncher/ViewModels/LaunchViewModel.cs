using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ClientLauncher.ViewModels
{
    public class LaunchViewModel : ViewModelBase
    {
        private readonly string _appCode;
        private readonly Window _window;
        private readonly IManifestService _manifestService;
        private readonly IInstallationService _installationService;
        private readonly IApiService _apiService;
        private readonly string _appsBasePath = @"C:\CompanyApps";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

        // Properties for update prompt
        private bool _showUpdatePrompt;
        public bool ShowUpdatePrompt
        {
            get => _showUpdatePrompt;
            set => SetProperty(ref _showUpdatePrompt, value);
        }

        private string _updateMessage = string.Empty;
        public string UpdateMessage
        {
            get => _updateMessage;
            set => SetProperty(ref _updateMessage, value);
        }

        private bool _forceUpdate;
        public bool ForceUpdate
        {
            get => _forceUpdate;
            set => SetProperty(ref _forceUpdate, value);
        }

        public RelayCommand CancelCommand { get; }
        public AsyncRelayCommand UpdateAndLaunchCommand { get; }
        public RelayCommand LaunchWithoutUpdateCommand { get; }

        public LaunchViewModel(string appCode, Window window)
        {
            _appCode = appCode;
            _window = window;
            _manifestService = new ManifestService();
            _installationService = new InstallationService();
            _apiService = new ApiService();

            CancelCommand = new RelayCommand(_ => Cancel());
            UpdateAndLaunchCommand = new AsyncRelayCommand(async _ => await UpdateAndLaunchAsync());
            LaunchWithoutUpdateCommand = new RelayCommand(_ => LaunchWithoutUpdate());

            _logger.Info($"LaunchViewModel initialized for {appCode}");

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

                // ✅ Step 2: Load local manifest
                StatusEmoji = "📄";
                StatusMessage = "Loading manifest...";
                ProgressValue = 10;

                var localManifest = await _manifestService.GetLocalManifestAsync(_appCode);

                if (localManifest == null)
                {
                    throw new Exception("Manifest not found. Please reinstall the application.");
                }

                var localVersion = localManifest.Binary?.Version ?? "0.0.0";
                _logger.Info($"Local manifest loaded: {_appCode} v{localVersion}");

                ProgressValue = 20;

                // ✅ Step 3: Download server manifest to check version
                StatusEmoji = "🔍";
                StatusMessage = "Checking for updates...";

                var serverManifest = await _manifestService.DownloadManifestFromServerAsync(_appCode);

                if (serverManifest == null)
                {
                    _logger.Warn("Could not fetch server manifest, proceeding with local version");
                    
                    // Check if app binary exists
                    var appPath = Path.Combine(_appsBasePath, _appCode, "App");
                    if (!Directory.Exists(appPath) || Directory.GetFiles(appPath, "*.exe").Length == 0)
                    {
                        // No binary installed, must download
                        _logger.Info("No binary found, downloading initial package...");
                        await DownloadAndInstallPackageAsync(localManifest);
                    }

                    await LaunchApplicationSequenceAsync();
                    return;
                }

                var serverVersion = serverManifest.Binary?.Version ?? "0.0.0";
                _logger.Info($"Server manifest loaded: {_appCode} v{serverVersion}");

                ProgressValue = 40;

                // ✅ Step 4: Compare versions
                var hasUpdate = IsNewerVersion(serverVersion, localVersion);

                // ✅ Check if binary exists
                var binaryPath = Path.Combine(_appsBasePath, _appCode, "App");
                var binaryExists = Directory.Exists(binaryPath) && Directory.GetFiles(binaryPath, "*.exe").Length > 0;

                if (!binaryExists)
                {
                    // ✅ First run - download package
                    _logger.Info("First run detected, downloading package...");
                    
                    StatusEmoji = "⬇️";
                    StatusMessage = "Downloading application (first run)...";
                    ProgressValue = 50;

                    await DownloadAndInstallPackageAsync(serverManifest);
                    
                    // Save updated manifest
                    await _manifestService.SaveManifestAsync(_appCode, serverManifest);

                    await LaunchApplicationSequenceAsync();
                    return;
                }

                if (hasUpdate)
                {
                    // ✅ Show update prompt
                    var forceUpdate = serverManifest.UpdatePolicy?.Force ?? false;

                    StatusEmoji = "⚠️";
                    IsProcessing = false;
                    IsIndeterminate = false;
                    ShowUpdatePrompt = true;
                    ForceUpdate = forceUpdate;

                    UpdateMessage = forceUpdate
                        ? $"🔴 CRITICAL UPDATE REQUIRED\n\n" +
                          $"Current version: {localVersion}\n" +
                          $"New version: {serverVersion}\n\n" +
                          $"This update is mandatory and must be installed before launching."
                        : $"📦 New version available!\n\n" +
                          $"Current version: {localVersion}\n" +
                          $"New version: {serverVersion}\n\n" +
                          $"Would you like to update now?";

                    StatusMessage = $"Update available: v{serverVersion}";

                    if (forceUpdate)
                    {
                        CanCancel = false;
                    }

                    return; // Wait for user action
                }

                // ✅ No update needed, launch directly
                _logger.Info("Application is up to date, launching...");
                ProgressValue = 60;
                await LaunchApplicationSequenceAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Launch sequence failed");
                StatusEmoji = "❌";
                StatusMessage = $"Error: {ex.Message}";
                IsProcessing = false;
                CanCancel = true;

                _window.WindowState = WindowState.Normal;
                _window.Show();
            }
        }

        private async Task UpdateAndLaunchAsync()
        {
            try
            {
                ShowUpdatePrompt = false;
                IsProcessing = true;
                IsIndeterminate = true;

                StatusEmoji = "⬇️";
                StatusMessage = "Downloading update...";
                ProgressValue = 50;

                // ✅ Get server manifest
                var serverManifest = await _manifestService.DownloadManifestFromServerAsync(_appCode);

                if (serverManifest == null)
                {
                    throw new Exception("Failed to get manifest from server");
                }

                _logger.Info($"Starting update: {_appCode} to v{serverManifest.Binary?.Version}");

                // ✅ Download and install package
                await DownloadAndInstallPackageAsync(serverManifest);

                // ✅ Save updated manifest
                await _manifestService.SaveManifestAsync(_appCode, serverManifest);

                ProgressValue = 80;
                StatusMessage = "Update completed successfully";
                await Task.Delay(500);

                await LaunchApplicationSequenceAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Update failed");

                StatusEmoji = "❌";
                StatusMessage = $"Update failed: {ex.Message}";
                IsProcessing = false;
                CanCancel = true;

                _window.WindowState = WindowState.Normal;
                _window.Show();
            }
        }

        private void LaunchWithoutUpdate()
        {
            if (ForceUpdate)
            {
                MessageBox.Show(
                    "This update is mandatory. You cannot launch the application without updating.",
                    "Update Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            ShowUpdatePrompt = false;
            IsProcessing = true;
            IsIndeterminate = true;

            _ = LaunchApplicationSequenceAsync();
        }

        private async Task LaunchApplicationSequenceAsync()
        {
            StatusEmoji = "🚀";
            StatusMessage = "Launching application...";
            ProgressValue = 90;

            await Task.Delay(500);

            LaunchApplication();

            _window.WindowState = WindowState.Minimized;
            _window.Hide();

            await Task.Delay(100);
            _window.Close();
        }

        /// <summary>
        /// ✅ Download and install package using manifest info
        /// </summary>
        private async Task DownloadAndInstallPackageAsync(ManifestDto manifest)
        {
            try
            {
                var packageName = _manifestService.GetPackageName(manifest);
                
                if (string.IsNullOrEmpty(packageName))
                {
                    throw new Exception("Package name not found in manifest");
                }

                _logger.Info($"Installing package: {packageName}");

                StatusMessage = $"Downloading {packageName}...";
                
                var result = await _installationService.InstallApplicationAsync(_appCode, packageName);

                if (!result.Success)
                {
                    throw new Exception($"Installation failed: {result.Message}");
                }

                _logger.Info($"Package installed successfully: {packageName}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Package installation failed");
                throw;
            }
        }

        private async Task GetAppInfoAsync()
        {
            try
            {
                var apps = await _apiService.GetAllApplicationsAsync();
                var app = apps.FirstOrDefault(a => a.AppCode == _appCode);

                if (app != null)
                {
                    AppName = app.Name;
                }
                else
                {
                    AppName = _appCode;
                }
            }
            catch
            {
                AppName = _appCode;
            }
        }

        private void LaunchApplication()
        {
            try
            {
                var appPath = Path.Combine(_appsBasePath, _appCode, "App");
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
                    _logger.Info($"Application launched: {exePath}");
                }
                else
                {
                    throw new Exception($"No executable found in {appPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to launch application");
                throw new Exception($"Failed to launch application: {ex.Message}", ex);
            }
        }

        private bool IsNewerVersion(string serverVersion, string localVersion)
        {
            try
            {
                var server = new Version(serverVersion);
                var local = new Version(localVersion);
                return server > local;
            }
            catch
            {
                return serverVersion != localVersion;
            }
        }

        private void Cancel()
        {
            _window.Close();
        }
    }
}