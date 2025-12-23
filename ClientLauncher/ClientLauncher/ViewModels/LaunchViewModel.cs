using ClientLauncher.Helpers;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ClientLauncher.ViewModels
{
    public class LaunchViewModel : ViewModelBase
    {
        private readonly string _appCode;
        private readonly Window _window;
        private readonly IVersionCheckService _versionCheckService;
        private readonly IApiService _apiService;
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
            _versionCheckService = new VersionCheckService();
            _apiService = new ApiService();

            CancelCommand = new RelayCommand(_ => Cancel());
            UpdateAndLaunchCommand = new AsyncRelayCommand(async _ => await UpdateAndLaunchAsync());
            LaunchWithoutUpdateCommand = new RelayCommand(_ => LaunchWithoutUpdate());

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

                var versionCheck = await _versionCheckService.CheckForUpdatesAsync(_appCode);

                ProgressValue = 40;

                // If update available, show prompt
                if (versionCheck.UpdateAvailable)
                {
                    StatusEmoji = "⚠️";
                    IsProcessing = false;
                    IsIndeterminate = false;
                    ShowUpdatePrompt = true;
                    ForceUpdate = versionCheck.ForceUpdate;

                    UpdateMessage = versionCheck.ForceUpdate
                        ? $"🔴 CRITICAL UPDATE REQUIRED\n\n" +
                          $"Current version: {versionCheck.LocalVersion}\n" +
                          $"New version: {versionCheck.ServerVersion}\n\n" +
                          $"This update is mandatory and must be installed before launching."
                        : $"📦 New version available!\n\n" +
                          $"Current version: {versionCheck.LocalVersion}\n" +
                          $"New version: {versionCheck.ServerVersion}\n\n" +
                          $"Would you like to update now?";

                    StatusMessage = versionCheck.Message;

                    // If force update, disable skip option
                    if (versionCheck.ForceUpdate)
                    {
                        CanCancel = false; // Can't cancel if force update
                    }

                    return; // Wait for user action
                }

                // No update needed, continue to launch
                ProgressValue = 60;
                await LaunchApplicationSequenceAsync();
            }
            catch (Exception ex)
            {
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

                // Call update API
                var userName = Environment.UserName;
                var result = await _apiService.InstallApplicationAsync(_appCode, userName);

                if (!result.Success)
                {
                    throw new Exception($"Update failed: {result.Message}");
                }

                ProgressValue = 80;
                StatusMessage = "Update completed successfully";
                await Task.Delay(3000);

                // Continue to launch
                await LaunchApplicationSequenceAsync();
            }
            catch (Exception ex)
            {
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

            await Task.Delay(2000);

            LaunchApplication();

            _window.WindowState = WindowState.Minimized;
            _window.Hide();

            await Task.Delay(100);
            _window.Close();
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
}