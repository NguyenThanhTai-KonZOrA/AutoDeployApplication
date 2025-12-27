using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using NLog;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ClientLauncher.ViewModels
{
    public class LaunchViewModel : ViewModelBase
    {
        private readonly IManifestService _manifestService;
        private readonly IVersionCheckService _versionCheckService;
        private readonly IInstallationService _installationService;
        private readonly IInstallationChecker _installationChecker;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _appCode = string.Empty;
        private string _statusMessage = "Initializing...";
        private double _progressValue;
        private bool _isProcessing = true;
        private readonly Window _window;

        public string AppCode
        {
            get => _appCode;
            set => SetProperty(ref _appCode, value);
        }

        private string _appName = "Loading...";
        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        private string _statusEmoji = "🚀";
        public string StatusEmoji
        {
            get => _statusEmoji;
            set => SetProperty(ref _statusEmoji, value);
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


        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                // ✅ FIX: Update on UI thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    SetProperty(ref _statusMessage, value);
                }, DispatcherPriority.Normal);
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                // ✅ FIX: Update on UI thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    SetProperty(ref _progressValue, value);
                }, DispatcherPriority.Normal);
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public LaunchViewModel(string appCode, Window window)
        {
            _appCode = appCode;
            _window = window;
            _manifestService = new ManifestService();
            _versionCheckService = new VersionCheckService();
            _installationService = new InstallationService();
            _installationChecker = new InstallationChecker();

            //_ = InitializeAndLaunchAsync();
            // Load after UI is ready
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(100); // Give UI time to render
                await InitializeAndLaunchAsync();
            }, DispatcherPriority.Background);
        }

        private async Task InitializeAndLaunchAsync()
        {
            try
            {
                Logger.Info("=== Starting launch process for {AppCode} ===", AppCode);

                // Step 1: Connecting to server
                UpdateStatus("🔌 Connecting to server...", 5);
                StatusEmoji = "🔌";
                await Task.Delay(100); // ✅ Increased delay for UI update

                var manifest = await _manifestService.GetManifestFromServerAsync(AppCode);
                if (manifest == null)
                {
                    Logger.Error("Failed to get manifest from server for {AppCode}", AppCode);
                    UpdateStatus("❌ Failed to connect to server", 0);
                    await Task.Delay(500);
                    Application.Current.Shutdown();
                    return;
                }

                Logger.Info("Manifest retrieved: Binary v{BinaryVersion}, Config v{ConfigVersion}",
                    manifest.Binary?.Version, manifest.Config?.Version);

                // Step 2: Checking installation status
                UpdateStatus("🔍 Checking installation status...", 15);
                StatusEmoji = "🔍";
                await Task.Delay(100);

                var isInstalled = _installationChecker.IsApplicationInstalled(AppCode);
                var currentVersion = _installationChecker.GetInstalledVersion(AppCode);

                Logger.Info("Installation check: IsInstalled={IsInstalled}, Version={Version}",
                    isInstalled, currentVersion);

                if (isInstalled && !string.IsNullOrEmpty(currentVersion))
                {
                    // ✅ FIX: App is installed, check for updates
                    UpdateStatus("✓ Application is installed", 25);
                    await Task.Delay(100);
                    StatusEmoji = "✅";
                    UpdateStatus("🔍 Checking for updates...", 35);
                    await Task.Delay(100);
                    StatusEmoji = "🔍";

                    var isUpdateAvailable = await _versionCheckService.IsUpdateAvailableAsync(AppCode);
                    var isForceUpdate = await _versionCheckService.IsForceUpdateRequiredAsync(AppCode);

                    if (isUpdateAvailable)
                    {
                        Logger.Info("Update available for {AppCode}. Force={IsForce}", AppCode, isForceUpdate);

                        if (isForceUpdate)
                        {
                            UpdateStatus("⚠️ Mandatory update required...", 40);
                            StatusEmoji = "⚠️";
                            await Task.Delay(100);
                            await PerformUpdateAsync(manifest);
                        }
                        else
                        {
                            StatusEmoji = "ℹ️";
                            var result = MessageBox.Show(
                                $"A new version ({manifest.Binary?.Version}) is available.\n\n" +
                                $"Current version: {currentVersion}\n" +
                                $"Do you want to update now?",
                                "Update Available",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            if (result == MessageBoxResult.Yes)
                            {
                                StatusEmoji = "✅";
                                await PerformUpdateAsync(manifest);
                            }
                            else
                            {
                                UpdateStatus("⏭️ Update skipped", 45);
                                StatusEmoji = "⏭️";
                                await Task.Delay(100);
                            }
                        }
                    }
                    else
                    {
                        // ✅ No update needed - LAUNCH DIRECTLY
                        UpdateStatus("✓ Application is up to date", 45);
                        StatusEmoji = "✅";
                        await Task.Delay(100);

                        Logger.Info("No update needed, launching application directly");
                    }

                    // ✅ FIX: Launch application directly without reinstalling
                    await LaunchApplicationAsync();
                }
                else
                {
                    // Not installed - perform installation
                    Logger.Info("Application not installed. Starting installation...");
                    UpdateStatus("📦 Application not installed. Installing...", 30);
                    StatusEmoji = "📦";
                    await Task.Delay(100);

                    await PerformInstallationAsync(manifest);

                    // Launch after installation
                    StatusEmoji = "🚀";
                    await LaunchApplicationAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Launch process failed for {AppCode}", AppCode);
                UpdateStatus($"❌ Error: {ex.Message}", 0);
                await Task.Delay(3000);
                Application.Current.Shutdown();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task PerformUpdateAsync(ManifestDto manifest)
        {
            try
            {
                Logger.Info("Starting update process for {AppCode}", AppCode);

                UpdateStatus("📥 Downloading update package...", 50);
                StatusEmoji = "📥";
                await Task.Delay(500);

                var updateType = manifest.UpdatePolicy?.Type ?? "both";
                Logger.Info("Update type: {UpdateType}", updateType);

                var result = await _installationService.UpdateApplicationAsync(
                    AppCode,
                    Environment.UserName);

                if (result.Success)
                {
                    Logger.Info("Update completed successfully");

                    UpdateStatus("📦 Extracting files...", 65);
                    StatusEmoji = "📦";
                    await Task.Delay(500);

                    UpdateStatus("✓ Update completed successfully", 75);
                    StatusEmoji = "✓";
                    await Task.Delay(500);

                    // Save updated manifest to C:\CompanyApps\{appCode}\manifest.json
                    await _manifestService.SaveManifestAsync(AppCode, manifest);
                }
                else
                {
                    Logger.Error("Update failed: {Message}", result.Message);
                    throw new Exception(result.Message ?? "Update failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update failed for {AppCode}", AppCode);
                throw;
            }
        }

        private async Task PerformInstallationAsync(ManifestDto manifest)
        {
            try
            {
                Logger.Info("Starting installation for {AppCode}", AppCode);

                UpdateStatus("📥 Downloading installation package...", 40);
                StatusEmoji = "📥";
                await Task.Delay(200);

                UpdateStatus("📦 Extracting application files...", 55);
                StatusEmoji = "📦";
                await Task.Delay(200);

                var result = await _installationService.InstallApplicationAsync(
                    AppCode,
                    Environment.UserName);

                if (result.Success)
                {
                    Logger.Info("Installation completed successfully");

                    UpdateStatus("⚙️ Configuring application...", 70);
                    StatusEmoji = "⚙️";
                    await Task.Delay(200);

                    UpdateStatus("✓ Installation completed successfully", 80);
                    StatusEmoji = "✓";
                    await Task.Delay(200);

                    // Save manifest to C:\CompanyApps\{appCode}\manifest.json
                    await _manifestService.SaveManifestAsync(AppCode, manifest);
                }
                else
                {
                    Logger.Error("Installation failed: {Message}", result.Message);
                    throw new Exception(result.Message ?? "Installation failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Installation failed for {AppCode}", AppCode);
                throw;
            }
        }

        private async Task LaunchApplicationAsync()
        {
            // Verifying installation
            UpdateStatus("🔍 Verifying installation...", 85);
            StatusEmoji = "🔍";
            await Task.Delay(100);

            // Launching application
            UpdateStatus("🚀 Launching application...", 92);
            StatusEmoji = "🚀";
            await Task.Delay(100);

            var appPath = GetApplicationPath();
            if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
            {
                Logger.Error("Application executable not found at: {Path}", appPath);
                UpdateStatus("❌ Application executable not found", 0);
                await Task.Delay(2000);
                Application.Current.Shutdown();
                return;
            }

            Logger.Info("Launching: {AppPath}", appPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(appPath)
            });

            StatusEmoji = "✓";
            UpdateStatus("✓ Application launched successfully!", 100);
            await Task.Delay(500);

            Logger.Info("=== Launch process completed for {AppCode} ===", AppCode);
            StatusEmoji = "🔒";
            Application.Current.Shutdown();
            _window.WindowState = WindowState.Minimized;
            _window.Hide();

            await Task.Delay(100);
            _window.Close();
        }

        private void Cancel()
        {
            _window.Close();
        }

        private string GetApplicationPath()
        {
            var appBasePath = Path.Combine(@"C:\CompanyApps", AppCode, "App");

            // Common executable patterns
            var possibleExes = new[]
            {
                Path.Combine(appBasePath, $"{AppCode}.exe"),
                Path.Combine(appBasePath, $"{AppCode}.Application.exe")
            };

            // Try exact matches first
            var exePath = possibleExes.FirstOrDefault(File.Exists);

            // If not found, search for any .exe
            if (string.IsNullOrEmpty(exePath) && Directory.Exists(appBasePath))
            {
                exePath = Directory.GetFiles(appBasePath, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            Logger.Debug("Application path for {AppCode}: {Path}", AppCode, exePath ?? "NOT FOUND");
            return exePath ?? string.Empty;
        }

        // ✅ Helper method to update status on UI thread
        private void UpdateStatus(string message, double progress)
        {
            StatusMessage = message;
            ProgressValue = progress;
            Logger.Debug("Status: {Message} ({Progress}%)", message, progress);
        }
    }
}