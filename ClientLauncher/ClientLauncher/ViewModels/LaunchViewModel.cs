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
                // Update on UI thread
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
                // Update on UI thread
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

            CancelCommand = new RelayCommand(Cancel);

            // FIX: Chỉ gọi 1 lần sau khi UI ready
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
                await Task.Delay(100);

                var manifest = await _manifestService.GetManifestFromServerAsync(AppCode);
                if (manifest == null)
                {
                    Logger.Error("Failed to get manifest from server for {AppCode}", AppCode);
                    UpdateStatus("❌ Failed to connect to server", 0);
                    await Task.Delay(2000);
                    Application.Current.Shutdown();
                    return;
                }

                Logger.Info("Manifest retrieved: Binary {BinaryVersion}, Config {ConfigVersion}",
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
                    // App is installed, check for updates
                    UpdateStatus("✓ Application is installed", 25);
                    StatusEmoji = "✅";
                    await Task.Delay(100);

                    UpdateStatus("🔍 Checking for updates...", 35);
                    StatusEmoji = "🔍";
                    await Task.Delay(100);

                    var isUpdateAvailable = await _versionCheckService.IsUpdateAvailableAsync(AppCode);
                    Logger.Info("Update available: {IsUpdateAvailable}", isUpdateAvailable);
                    var isForceUpdate = await _versionCheckService.IsForceUpdateRequiredAsync(AppCode);

                    if (isUpdateAvailable)
                    {
                        Logger.Info("Update available for {AppCode}. Force={IsForce}", AppCode, isForceUpdate);

                        bool shouldUpdate = isForceUpdate;

                        if (!isForceUpdate)
                        {
                            StatusEmoji = "ℹ️";
                            var result = MessageBox.Show(
                                $"A new version ({manifest.Binary?.Version}) is available.\n\n" +
                                $"Current version: {currentVersion}\n" +
                                $"Do you want to update now?",
                                "Update Available",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            shouldUpdate = (result == MessageBoxResult.Yes);
                        }

                        if (shouldUpdate)
                        {
                            UpdateStatus("⚠️ Updating application...", 40);
                            StatusEmoji = "⚠️";
                            await Task.Delay(100);

                            // FIX: Perform update and verify
                            var updateResult = await PerformUpdateAsync(manifest);

                            if (!updateResult)
                            {
                                Logger.Error("Update failed, aborting launch");
                                UpdateStatus("❌ Update failed. Using current version.", 0);
                                await Task.Delay(3000);
                                Application.Current.Shutdown();
                                return;
                            }
                        }
                        else
                        {
                            UpdateStatus("⏭️ Update skipped", 45);
                            StatusEmoji = "⏭️";
                            await Task.Delay(100);
                        }
                    }
                    else
                    {
                        UpdateStatus("✓ Application is up to date", 45);
                        StatusEmoji = "✅";
                        await Task.Delay(100);
                        Logger.Info("No update needed, launching application directly");
                    }

                    // Launch application
                    await LaunchApplicationAsync();
                }
                else
                {
                    // Not installed - perform installation
                    Logger.Info("Application not installed. Starting installation...");
                    UpdateStatus("📦 Application not installed. Installing...", 30);
                    StatusEmoji = "📦";
                    await Task.Delay(100);

                    var installResult = await PerformInstallationAsync(manifest);

                    if (!installResult)
                    {
                        Logger.Error("Installation failed, aborting launch");
                        UpdateStatus("❌ Installation failed", 0);
                        await Task.Delay(3000);
                        Application.Current.Shutdown();
                        return;
                    }

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

        private async Task<bool> PerformUpdateAsync(ManifestDto manifest)
        {
            try
            {
                Logger.Info("Starting update process for {AppCode}", AppCode);

                UpdateStatus("📥 Downloading update package...", 50);
                StatusEmoji = "📥";
                await Task.Delay(300);

                var updateType = manifest.UpdatePolicy?.Type ?? "both";
                Logger.Info("Update type: {UpdateType}", updateType);

                var result = await _installationService.UpdateApplicationAsync(
                    AppCode,
                    Environment.UserName);

                if (!result.Success)
                {
                    Logger.Error("Update failed: {Message}", result.Message);

                    // Show error message to user
                    MessageBox.Show(
                        result.Message ?? "Cannot update application. Please try again later.",
                        "Update Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                Logger.Info("Update completed successfully");

                UpdateStatus("📦 Extracting files...", 65);
                StatusEmoji = "📦";
                await Task.Delay(300);

                UpdateStatus("✓ Update completed successfully", 75);
                StatusEmoji = "✓";
                await Task.Delay(300);

                // ✅ CRITICAL: Verify installation TRƯỚC KHI save version
                UpdateStatus("🔍 Verifying installation...", 80);
                StatusEmoji = "🔍";
                await Task.Delay(200);

                if (!await VerifyInstallationAsync())
                {
                    Logger.Error("Verification failed after update - executable not found");

                    // ❌ Trigger rollback
                    await TriggerRollbackAsync(result.UpdatedManifest?.Binary?.Version ?? "unknown");

                    MessageBox.Show(
                        "Bản cập nhật có lỗi (không tìm thấy file thực thi).\n" +
                        "Hệ thống đã tự động khôi phục về phiên bản cũ.\n" +
                        "Vui lòng liên hệ IT support.",
                        "Update Verification Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                // ✅ Chỉ save version SAU KHI verify exe thành công
                UpdateStatus("💾 Saving version info...", 85);
                StatusEmoji = "💾";

                await SaveVersionAndManifestAsync(result.UpdatedManifest ?? manifest);

                Logger.Info("Version and manifest saved successfully");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update failed for {AppCode}", AppCode);

                MessageBox.Show(
                    $"Lỗi khi cập nhật: {ex.Message}\n" +
                    "Hệ thống đã tự động khôi phục về phiên bản cũ.",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        /// <summary>
        /// Save version info and manifest after successful verification
        /// </summary>
        private async Task SaveVersionAndManifestAsync(ManifestDto manifest)
        {
            try
            {
                // Save version.txt
                var versionFile = Path.Combine(@"C:\CompanyApps", AppCode, "App", "version.txt");
                var versionDir = Path.GetDirectoryName(versionFile);

                if (!string.IsNullOrEmpty(versionDir) && !Directory.Exists(versionDir))
                {
                    Directory.CreateDirectory(versionDir);
                }

                await File.WriteAllTextAsync(versionFile, manifest.Binary?.Version ?? "0.0.0");
                Logger.Info("Saved version {Version} to {Path}", manifest.Binary?.Version, versionFile);

                // Save manifest.json
                await _manifestService.SaveManifestAsync(AppCode, manifest);
                Logger.Info("Saved manifest for {AppCode}", AppCode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save version and manifest");
                throw;
            }
        }

        /// <summary>
        /// Trigger rollback when verification fails
        /// </summary>
        private async Task TriggerRollbackAsync(string failedVersion)
        {
            try
            {
                Logger.Warn("Triggering rollback due to verification failure for version {Version}", failedVersion);

                // Call installation service để rollback
                var currentVersion = _installationChecker.GetInstalledVersion(AppCode);

                // Mark update as failed
                var failureMarkerPath = Path.Combine(@"C:\CompanyApps", AppCode, ".update_failed");
                var failureData = new
                {
                    FailedVersion = failedVersion,
                    Timestamp = DateTime.UtcNow,
                    MachineName = Environment.MachineName,
                    ErrorType = "VerificationFailed_MissingExecutable"
                };

                await File.WriteAllTextAsync(failureMarkerPath,
                    System.Text.Json.JsonSerializer.Serialize(failureData));

                Logger.Info("Marked update as failed for version {Version}", failedVersion);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to trigger rollback");
            }
        }

        private async Task<bool> PerformInstallationAsync(ManifestDto manifest)
        {
            try
            {
                Logger.Info("Starting installation for {AppCode}", AppCode);

                UpdateStatus("📥 Downloading installation package...", 40);
                StatusEmoji = "📥";
                await Task.Delay(100);

                UpdateStatus("📦 Extracting application files...", 55);
                StatusEmoji = "📦";
                await Task.Delay(100);

                var result = await _installationService.InstallApplicationAsync(
                    AppCode,
                    Environment.UserName);

                if (!result.Success)
                {
                    Logger.Error("Installation failed: {Message}", result.Message);

                    MessageBox.Show(
                        result.Message ?? "Không thể cài đặt ứng dụng.",
                        "Installation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                Logger.Info("Installation completed successfully");

                UpdateStatus("⚙️ Configuring application...", 70);
                StatusEmoji = "⚙️";
                await Task.Delay(100);

                UpdateStatus("✓ Installation completed successfully", 80);
                StatusEmoji = "✓";
                await Task.Delay(300);

                // Verify installation
                if (!await VerifyInstallationAsync())
                {
                    Logger.Error("Verification failed after installation - executable not found");

                    MessageBox.Show(
                        "Version update failed (executable not found).\n" +
                        "The system has automatically rolled back to the previous version.\n" +
                        "Please contact IT support.",
                        "Installation Verification Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

                // Save manifest
                await _manifestService.SaveManifestAsync(AppCode, manifest);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Installation failed for {AppCode}", AppCode);

                MessageBox.Show(
                    $"Have an error when install application: {ex.Message}",
                    "Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        /// <summary>
        /// Verify that the executable exists after installation/update
        /// </summary>
        private async Task<bool> VerifyInstallationAsync()
        {
            await Task.Delay(100); // Small delay to ensure file system is updated

            var appPath = GetApplicationPath();
            var isValid = !string.IsNullOrEmpty(appPath) && File.Exists(appPath);

            Logger.Info("Installation verification: {IsValid}, Path: {Path}", isValid, appPath ?? "NOT FOUND");

            return isValid;
        }

        private async Task LaunchApplicationAsync()
        {
            try
            {
                // Verifying installation
                UpdateStatus("🔍 Verifying installation...", 85);
                StatusEmoji = "🔍";
                await Task.Delay(200);

                var appPath = GetApplicationPath();
                if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
                {
                    Logger.Error("Application executable not found at: {Path}", appPath ?? "NULL");

                    // Log all files in App directory for debugging
                    var appDir = Path.Combine(@"C:\CompanyApps", AppCode, "App");
                    if (Directory.Exists(appDir))
                    {
                        var files = Directory.GetFiles(appDir, "*.*", SearchOption.AllDirectories);
                        Logger.Error("Files in App directory: {Files}", string.Join(", ", files));
                    }
                    else
                    {
                        Logger.Error("App directory does not exist: {Path}", appDir);
                    }

                    UpdateStatus("❌ Application executable not found", 0);

                    MessageBox.Show(
                        "Cannot find application executable.\n" +
                        "Please check:\n" +
                        $"1. Package on server contains .exe file\n" +
                        $"2. Path: {appDir}\n" +
                        "3. Contact IT support if the issue persists.",
                        "Launch Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    await Task.Delay(2000);
                    Application.Current.Shutdown();
                    return;
                }

                // Launching application
                UpdateStatus("🚀 Launching application...", 92);
                StatusEmoji = "🚀";
                await Task.Delay(200);

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

                // Close launcher window
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _window.WindowState = WindowState.Minimized;
                    _window.Hide();
                    _window.Close();
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to launch application for {AppCode}", AppCode);

                MessageBox.Show(
                    $"Cannot launch application: {ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();
            }
        }

        private void Cancel(object? parameter)
        {
            Logger.Info("User cancelled launch process");
            _window.Close();
        }

        private string GetApplicationPath()
        {
            var appBasePath = Path.Combine(@"C:\CompanyApps", AppCode, "App");

            if (!Directory.Exists(appBasePath))
            {
                Logger.Warn("App directory does not exist: {Path}", appBasePath);
                return string.Empty;
            }

            // Common executable patterns
            var possibleExes = new[]
            {
                Path.Combine(appBasePath, $"{AppCode}.exe"),
                Path.Combine(appBasePath, $"{AppCode}.Application.exe")
            };

            // Try exact matches first
            var exePath = possibleExes.FirstOrDefault(File.Exists);

            // If not found, search for any .exe in root directory
            if (string.IsNullOrEmpty(exePath))
            {
                var exeFiles = Directory.GetFiles(appBasePath, "*.exe", SearchOption.TopDirectoryOnly);
                exePath = exeFiles.FirstOrDefault();

                if (exeFiles.Length > 1)
                {
                    Logger.Warn("Multiple exe files found: {Files}", string.Join(", ", exeFiles));
                }
            }

            Logger.Debug("Application path for {AppCode}: {Path}", AppCode, exePath ?? "NOT FOUND");
            return exePath ?? string.Empty;
        }

        // Helper method to update status on UI thread
        private void UpdateStatus(string message, double progress)
        {
            StatusMessage = message;
            ProgressValue = progress;
            Logger.Debug("Status: {Message} ({Progress}%)", message, progress);
        }
    }
}