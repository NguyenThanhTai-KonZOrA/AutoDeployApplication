using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ClientLauncher.ViewModels
{
    public class LaunchViewModel : ViewModelBase
    {
        #region Init Contructor and Services
        private readonly IManifestService _manifestService;
        private readonly IVersionCheckService _versionCheckService;
        private readonly IInstallationService _installationService;
        private readonly IInstallationChecker _installationChecker;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _appBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
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

            // Initialize and launch after UI ready
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(100); // Give UI time to render
                await InitializeAndLaunchAsync();
            }, DispatcherPriority.Background);
        }
        #endregion

        #region Main Launch Logic

        /// <summary>
        /// InitializeAndLaunchAsync
        /// </summary>
        /// <returns></returns>
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

                            // ✅ Perform update with Verify-then-Commit flow
                            var updateSuccess = await PerformUpdateAsync(manifest);

                            if (!updateSuccess)
                            {
                                Logger.Error("Update failed, aborting launch");
                                UpdateStatus("❌ Update failed. Using current version.", 0);
                                await Task.Delay(2000);

                                UpdateStatus("ℹ️ We will open the previous version.", 100);
                                await Task.Delay(2000);
                                await LaunchApplicationAsync();
                                // Uncommented this line to ensure shutdown after launching previous version
                                Application.Current.Shutdown();
                                return;
                            }

                            await _installationService.NotifyInstallationAsync(AppCode, manifest.Binary.Version, true, TimeSpan.Zero,
                            "✓ Application is updated successfully",
                            "0.0.0", "Update");
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

                    var installSuccess = await PerformInstallationAsync(manifest);

                    if (!installSuccess)
                    {
                        Logger.Error("Installation failed, aborting launch");
                        UpdateStatus("❌ Installation failed", 0);
                        await Task.Delay(2000);
                        Application.Current.Shutdown();

                        await _installationService.NotifyInstallationAsync(AppCode, manifest.Binary.Version, false, TimeSpan.Zero,
                            "❌ Installation failed: Verification failed after installation - executable not found",
                            "0.0.0", "Install");
                        return;
                    }

                    await _installationService.NotifyInstallationAsync(AppCode, manifest.Binary.Version, true, TimeSpan.Zero,
                           "✓ Application is installed successfully",
                           "0.0.0", "Install");

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

        /// <summary>
        /// Perform update with Verify-then-Commit flow with Rollback support
        /// </summary>
        private async Task<bool> PerformUpdateAsync(ManifestDto manifest)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Logger.Info("Starting update process for {AppCode}", AppCode);

                UpdateStatus("📥 Downloading update package...", 50);
                StatusEmoji = "📥";
                await Task.Delay(100);

                // ✅ Step 1: Download to NewVersion folder
                var updateResult = await _installationService.UpdateApplicationAsync(
                    AppCode,
                    Environment.UserName);

                if (!updateResult.Success)
                {
                    Logger.Error("Update download failed: {Message}", updateResult.Message);

                    MessageBox.Show(
                        updateResult.Message ?? "Cannot download the update. Please try again later.",
                        "Update Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                Logger.Info("Update package downloaded successfully to: {Path}", updateResult.TempAppPath);

                UpdateStatus("📦 Extracting files...", 65);
                StatusEmoji = "📦";
                await Task.Delay(100);

                // ✅ Step 2: Verify exe trong NewVersion folder (only for binary updates)
                var updateType = manifest.UpdatePolicy?.Type ?? "both";

                if (updateType == "binary" || updateType == "both")
                {
                    UpdateStatus("🔍 Verifying installation...", 75);
                    StatusEmoji = "🔍";
                    await Task.Delay(100);

                    if (!await VerifyInstallationInNewVersionAsync(updateResult.TempAppPath))
                    {
                        Logger.Error("Verification failed - executable not found in new version folder");

                        // Clean up new version folder
                        if (!string.IsNullOrEmpty(updateResult.TempAppPath) && Directory.Exists(updateResult.TempAppPath))
                        {
                            try
                            {
                                Directory.Delete(updateResult.TempAppPath, true);
                                Logger.Info("Deleted new version folder after verification failure");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn(ex, "Failed to delete new version folder");
                            }
                        }

                        // Delete backup (no rollback needed since the App wasn't overwritten)
                        if (!string.IsNullOrEmpty(updateResult.BackupPath) && Directory.Exists(updateResult.BackupPath))
                        {
                            try
                            {
                                Directory.Delete(updateResult.BackupPath, true);
                                Logger.Info("Deleted backup after verification failure");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn(ex, "Failed to delete backup");
                            }
                        }

                        MessageBox.Show(
                            "The update has errors (executable not found).\n" +
                            "The current version will continue to operate normally.",
                            "Update Verification Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return false;
                    }

                    Logger.Info("✅ Binary verification passed");
                }
                else
                {
                    Logger.Info("Config-only update, skipping binary verification");
                }

                // Step 3: Commit (move from NewVersion to App + save version/manifest)
                UpdateStatus("💾 Installing new version...", 85);
                StatusEmoji = "💾";
                await Task.Delay(200);

                if (updateResult.UpdatedManifest != null &&
                    !string.IsNullOrEmpty(updateResult.BackupPath))
                {
                    var commitSuccess = await _installationService.CommitUpdateAsync(
                        AppCode,
                        updateResult.UpdatedManifest,
                        updateResult.BackupPath,
                        updateResult.TempAppPath ?? string.Empty);

                    if (!commitSuccess)
                    {
                        Logger.Error("❌ Failed to commit update - INITIATING ROLLBACK");

                        UpdateStatus("⚠️ Update failed. Rolling back to previous version...", 80);
                        StatusEmoji = "⚠️";
                        stopwatch.Stop();

                        await Task.Delay(300);

                        await _installationService.NotifyInstallationAsync(
                            AppCode,
                            manifest.Binary?.Version ?? "0.0.0",
                            false,
                            stopwatch.Elapsed,
                            "❌ Failed to commit update - INITIATING ROLLBACK: ⚠️ Update failed. Rolling back to previous version... ",
                           _installationService.GetVersionFromBackup(updateResult.BackupPath),
                            "Update"
                        );

                        // 🔥 ROLLBACK
                        try
                        {
                            var rollbackSuccess = await _installationService.RollbackUpdateAsync(
                                AppCode,
                                updateResult.BackupPath,
                                updateResult.UpdatedManifest.Binary?.Version ?? "unknown");

                            if (rollbackSuccess)
                            {
                                Logger.Info("✅ Rollback successful - App restored to previous version");

                                UpdateStatus("✓ Rollback completed. Previous version restored.", 100);
                                StatusEmoji = "✅";
                                await Task.Delay(500);

                                MessageBox.Show(
                                    "Update failed, but your application has been restored to the previous version.\n" +
                                    "The application will launch normally with the previous version.",
                                    "Rollback Successful",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                                return false;
                            }
                            else
                            {
                                Logger.Error("❌ CRITICAL: Rollback also failed!");

                                UpdateStatus("❌ Critical error: Rollback failed", 0);
                                StatusEmoji = "❌";
                                await Task.Delay(1000);

                                var appPath = Path.Combine(_appBasePath, AppCode, "App");
                                var backupLocation = updateResult.BackupPath;

                                MessageBox.Show(
                                    "⚠️ CRITICAL ERROR ⚠️\n\n" +
                                    "Update failed and automatic rollback also failed.\n\n" +
                                    "MANUAL RECOVERY NEEDED:\n" +
                                    $"1. Backup location: {backupLocation}\n" +
                                    $"2. App location: {appPath}\n\n" +
                                    "Please contact IT support immediately.",
                                    "Critical Update Error - Manual Recovery Required",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);

                                // Don't try to launch - app might be broken
                                Application.Current.Shutdown();
                                return false;
                            }
                        }
                        catch (Exception rollbackEx)
                        {
                            Logger.Error(rollbackEx, "❌ Exception during rollback attempt");

                            MessageBox.Show(
                                $"CRITICAL: Rollback exception:\n{rollbackEx.Message}\n\n" +
                                $"Backup: {updateResult.BackupPath}\n" +
                                "Contact IT support immediately.",
                                "Rollback Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            Application.Current.Shutdown();
                            return false;
                        }
                    }

                    // ✅ COMMIT SUCCESS - NOW VERIFY BEFORE CLEANUP
                    Logger.Info("✅ Commit successful - Now verifying new version can launch");

                    UpdateStatus("🔍 Verifying new version...", 88);
                    StatusEmoji = "🔍";
                    await Task.Delay(300);

                    // 🔥 CRITICAL: Verify BEFORE deleting backup
                    var canLaunch = await VerifyNewVersionCanLaunchAsync();

                    if (!canLaunch)
                    {
                        Logger.Error("❌ New version verification FAILED - .exe not found or corrupted");

                        UpdateStatus("⚠️ New version has errors. Rolling back...", 80);
                        StatusEmoji = "⚠️";
                        await Task.Delay(500);

                        // 🔥 ROLLBACK because verification failed
                        var rollbackSuccess = await _installationService.RollbackUpdateAsync(
                            AppCode,
                            updateResult.BackupPath,
                            updateResult.UpdatedManifest.Binary?.Version ?? "unknown");

                        if (rollbackSuccess)
                        {
                            Logger.Info("✅ Rollback successful after verification failure");

                            UpdateStatus("✓ Rollback completed. Previous version restored.", 100);
                            StatusEmoji = "✅";
                            await Task.Delay(500);

                            MessageBox.Show(
                                "The new version has errors and cannot be launched.\n" +
                                "Your application has been restored to the previous version.\n" +
                                "The application will launch normally.",
                                "Update Verification Failed - Rollback Successful",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            return false; // Launch old version
                        }
                        else
                        {
                            Logger.Error("❌ CRITICAL: Rollback failed after verification failure!");

                            MessageBox.Show(
                                "CRITICAL ERROR:\n" +
                                "New version verification failed and rollback also failed.\n\n" +
                                $"Backup location: {updateResult.BackupPath}\n" +
                                "Contact IT support immediately.",
                                "Critical Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            Application.Current.Shutdown();
                            return false;
                        }
                    }

                    // ✅ VERIFICATION PASSED - Safe to delete backup now
                    Logger.Info("✅ New version verification PASSED - Finalizing update");

                    UpdateStatus("🗑️ Cleaning up...", 92);
                    StatusEmoji = "🗑️";
                    await Task.Delay(100);

                    await _installationService.FinalizeUpdateAsync(AppCode, updateResult.BackupPath);

                    Logger.Info("✅ Update committed successfully - Version: {Version}",
                        updateResult.UpdatedManifest.Binary?.Version);
                }

                UpdateStatus("✓ Update completed successfully", 95);
                StatusEmoji = "✅";
                await Task.Delay(100);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ Unexpected error during update for {AppCode}", AppCode);

                UpdateStatus("❌ Update error occurred", 0);
                StatusEmoji = "❌";

                MessageBox.Show(
                    $"Update error: {ex.Message}\n" +
                    "The current version will be used.",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        /// <summary>
        /// Verify exe trong NewVersion folder
        /// </summary>
        private async Task<bool> VerifyInstallationInNewVersionAsync(string? newVersionPath)
        {
            await Task.Delay(100);

            if (string.IsNullOrEmpty(newVersionPath) || !Directory.Exists(newVersionPath))
            {
                Logger.Error("New version folder not found: {Path}", newVersionPath ?? "NULL");
                return false;
            }

            // Find exe in new version folder
            var exeFiles = Directory.GetFiles(newVersionPath, "*.exe", SearchOption.TopDirectoryOnly);

            if (exeFiles.Length == 0)
            {
                Logger.Error("No exe file found in new version folder: {Path}", newVersionPath);

                // Log all files for debugging
                var allFiles = Directory.GetFiles(newVersionPath, "*.*", SearchOption.AllDirectories);
                Logger.Error("Files in new version folder: {Files}", string.Join(", ", allFiles.Select(Path.GetFileName)));

                return false;
            }

            Logger.Info("Found {Count} exe file(s) in new version folder: {Files}",
                exeFiles.Length,
                string.Join(", ", exeFiles.Select(Path.GetFileName)));

            return true;
        }

        /// <summary>
        /// Verify exe trong TEMP folder
        /// </summary>
        private async Task<bool> VerifyInstallationInTempAsync(string? tempAppPath)
        {
            await Task.Delay(100);

            if (string.IsNullOrEmpty(tempAppPath) || !Directory.Exists(tempAppPath))
            {
                Logger.Error("Temp folder not found: {Path}", tempAppPath ?? "NULL");
                return false;
            }

            // Find exe in temp folder
            var exeFiles = Directory.GetFiles(tempAppPath, "*.exe", SearchOption.TopDirectoryOnly);

            if (exeFiles.Length == 0)
            {
                Logger.Error("No exe file found in temp folder: {Path}", tempAppPath);
                return false;
            }

            Logger.Info("Found {Count} exe file(s) in temp: {Files}", exeFiles.Length, string.Join(", ", exeFiles));
            return true;
        }

        /// <summary>
        /// Perform installation for new app
        /// </summary>
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
                       "Cannot find application executable.\n" +
                       "Please check:\n" +
                       $"1. Package on server contains .exe file\n" +
                       $"2. Application Code: {AppCode}\n" +
                       $"3. Please ensure you have proper access rights to the application directory.\n" +
                       "4. Contact IT support if the issue persists.",
                       "Launch Failed",
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
                await Task.Delay(100);

                // Verify installation
                if (!await VerifyInstallationAsync())
                {
                    Logger.Error("Verification failed after installation - executable not found");

                    MessageBox.Show(
                        "Installation failed (executable not found).\n" +
                        "Please contact IT support.",
                        "Installation Verification Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return false;
                }

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

        /// <summary>
        /// Launch the application
        /// </summary>
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
                    var appDir = Path.Combine(_appBasePath, AppCode, "App");
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

                    await Task.Delay(1000);
                    Application.Current.Shutdown();
                    return;
                }

                // Launching application
                UpdateStatus("🚀 Launching application...", 92);
                StatusEmoji = "🚀";
                await Task.Delay(100);

                Logger.Info("Launching: {AppPath}", appPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(appPath)
                });

                StatusEmoji = "✓";
                UpdateStatus("✓ Application launched successfully!", 100);
                await Task.Delay(200);

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

        /// <summary>
        /// Cancel launch process
        /// </summary>
        private void Cancel(object? parameter)
        {
            Logger.Info("User cancelled launch process");
            _window.Close();
        }

        /// <summary>
        /// Get application executable path
        /// </summary>
        private string GetApplicationPath()
        {
            var appBasePath = Path.Combine(_appBasePath, AppCode, "App");

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

        /// <summary>
        /// Update status message and progress on UI thread
        /// </summary>
        private void UpdateStatus(string message, double progress)
        {
            StatusMessage = message;
            ProgressValue = progress;
            Logger.Debug("Status: {Message} ({Progress}%)", message, progress);
        }

        /// <summary>
        /// Verify new version can be launched (check .exe exists)
        /// </summary>
        private async Task<bool> VerifyNewVersionCanLaunchAsync()
        {
            await Task.Delay(100);

            var appPath = GetApplicationPath();

            if (string.IsNullOrEmpty(appPath) || !File.Exists(appPath))
            {
                Logger.Error("❌ Verification FAILED - Application executable not found");
                Logger.Error("Expected .exe at: {Path}", appPath ?? "NULL");

                // Log all files for debugging
                var appDir = Path.Combine(_appBasePath, AppCode, "App");
                if (Directory.Exists(appDir))
                {
                    var files = Directory.GetFiles(appDir, "*.*", SearchOption.AllDirectories);
                    Logger.Error("Files in App directory ({Count}): {Files}",
                        files.Length,
                        string.Join(", ", files.Select(Path.GetFileName)));
                }

                return false;
            }

            Logger.Info("✅ Verification PASSED - Executable found at: {Path}", appPath);
            return true;
        }
        #endregion
    }
}