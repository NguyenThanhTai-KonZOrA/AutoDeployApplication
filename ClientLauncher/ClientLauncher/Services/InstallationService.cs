using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class InstallationService : IInstallationService
    {
        #region Contructor
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _appBasePath;
        private readonly IManifestService _manifestService;
        private readonly ISelectiveUpdateService _selectiveUpdateService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public InstallationService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _appBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
            _manifestService = new ManifestService();
            _selectiveUpdateService = new SelectiveUpdateService(_manifestService);

            if (!Directory.Exists(_appBasePath))
            {
                Directory.CreateDirectory(_appBasePath);
            }

            Logger.Debug("InstallationService initialized with base path: {Path}", _appBasePath);
        }
        #endregion

        #region Public Interface Methods
        /// <summary>
        /// Install Application
        /// /// </summary>
        /// <param name="appCode"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<InstallationResult> InstallApplicationAsync(string appCode, string userName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Logger.Info("Starting installation for {AppCode}", appCode);

                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Failed to retrieve manifest from server");
                }

                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var binaryPackage = manifest.Binary?.Package;

                if (string.IsNullOrEmpty(binaryPackage))
                {
                    throw new Exception("Invalid manifest: binary package not specified");
                }

                Logger.Info("Downloading binary package: {Package}", binaryPackage);
                await DownloadAndExtractAsync(appCode, binaryPackage, appPath);

                Logger.Info("manifest config: {version}", manifest.Config.Version);
                if (string.IsNullOrEmpty(manifest.Config.Version))
                {
                    // Init Local
                    manifest.Config.Version = "0.0.0";
                    var configPath = Path.Combine(_appBasePath, appCode, "Config");

                    // Create Config directory if it doesn't exist
                    if (!Directory.Exists(configPath))
                    {
                        Directory.CreateDirectory(configPath);
                        Logger.Info("Created Config directory at {ConfigPath}", configPath);
                    }

                    Logger.Info("manifest config: {version}", manifest.Config.Version);
                    var versionFile = Path.Combine(_appBasePath, appCode, "Config", "version.txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(versionFile)!);
                    File.WriteAllText(versionFile, manifest.Config.Version);
                    Logger.Info("Created manifest config: {version}", manifest.Config.Version);
                }

                if (!string.IsNullOrEmpty(manifest.Config?.Package) &&
                    manifest.UpdatePolicy?.Type != "binary")
                {
                    var configPath = Path.Combine(_appBasePath, appCode, "Config");
                    Logger.Info("Downloading config package: {Package}", manifest.Config.Package);
                    await DownloadAndExtractAsync(appCode, manifest.Config.Package, configPath);
                }

                SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");
                await _manifestService.SaveManifestAsync(appCode, manifest);

                Logger.Info("Installation completed for {AppCode}", appCode);
                stopwatch.Stop();

                var result = new InstallationResult
                {
                    Success = true,
                    InstalledVersion = manifest.Binary?.Version,
                    Message = $"Installation completed successfully in {stopwatch.Elapsed.TotalSeconds:F2}s",
                    InstallationPath = appPath
                };

                //await NotifyInstallationAsync(
                //    appCode,
                //    result.InstalledVersion ?? string.Empty,
                //    true,
                //    stopwatch.Elapsed,
                //    null,
                //    null,
                //    "Install");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await NotifyInstallationAsync(
                    appCode,
                    "0.0.0",
                    false,
                    stopwatch.Elapsed,
                    ex.Message,
                    null,
                    "Install");

                Logger.Error(ex, "Installation failed for {AppCode}", appCode);

                return new InstallationResult
                {
                    Success = false,
                    Message = "Installation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Update Application
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName)
        {
            var stopwatch = Stopwatch.StartNew();
            string? currentBinaryVersion = null;
            string? newVersion = null;
            string? backupPath = null;
            string? newVersionPath = null;

            Logger.Info("Starting ClearUpdateFailureMarkerAsync for {AppCode}", appCode);
            await ClearUpdateFailureMarkerAsync(appCode);

            try
            {
                Logger.Info("Starting update for {AppCode}", appCode);

                currentBinaryVersion = GetCurrentVersion(appCode, "App");
                Logger.Info("Current version: {Version}", currentBinaryVersion ?? "N/A");

                if (await HasUpdateFailedBeforeAsync(appCode))
                {
                    Logger.Warn("Update was previously attempted and failed for {AppCode}", appCode);
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "The new version has errors. The system has automatically rolled back to the previous version. Please try again later.",
                        ErrorDetails = "Previous update attempt failed, automatic rollback applied",
                        InstalledVersion = currentBinaryVersion
                    };
                }

                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Failed to retrieve manifest from server");
                }

                newVersion = manifest.Binary?.Version;
                var updateType = manifest.UpdatePolicy?.Type ?? "both";
                Logger.Info("Update type: {UpdateType}, New version: {NewVersion}", updateType, newVersion);

                // Use app-specific folder instead of system temp
                var updateBasePath = Path.Combine(_appBasePath, appCode, "Updates");
                Directory.CreateDirectory(updateBasePath);

                backupPath = Path.Combine(updateBasePath, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");
                newVersionPath = Path.Combine(updateBasePath, $"NewVersion_{DateTime.Now:yyyyMMddHHmmss}");

                // Backup current installation
                await CreateBackupAsync(appCode, backupPath);

                try
                {
                    // Download to dedicated NewVersion folder
                    await DownloadNewVersionAsync(appCode, manifest, updateType, newVersionPath);

                    Logger.Info("Package downloaded to: {Path}", newVersionPath);

                    stopwatch.Stop();

                    // Return new version path for verification
                    return new InstallationResult
                    {
                        Success = true,
                        Message = $"Downloaded version {newVersion}. Checking stability...",
                        InstalledVersion = currentBinaryVersion,
                        UpdatedManifest = manifest,
                        BackupPath = backupPath,
                        TempAppPath = newVersionPath
                    };
                }
                catch (Exception updateEx)
                {
                    Logger.Error(updateEx, "Update failed during download for {AppCode}", appCode);

                    // Clean up new version folder
                    DeleteFolderSafely(newVersionPath, "new version");

                    stopwatch.Stop();

                    await NotifyInstallationAsync(
                        appCode,
                        currentBinaryVersion ?? "unknown",
                        true,
                        stopwatch.Elapsed,
                        $"Update to {newVersion} failed during download: {updateEx.Message}",
                        currentBinaryVersion,
                        "UpdateRollback");

                    DeleteBackupSafely(backupPath);

                    return new InstallationResult
                    {
                        Success = false,
                        Message = $"Cannot download the new update ({newVersion}).\nPlease try again later.",
                        ErrorDetails = $"Download error: {updateEx.Message}",
                        InstalledVersion = currentBinaryVersion
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update failed for {AppCode}", appCode);
                stopwatch.Stop();

                await NotifyInstallationAsync(
                    appCode,
                    currentBinaryVersion ?? string.Empty,
                    true,
                    stopwatch.Elapsed,
                    ex.Message,
                    currentBinaryVersion,
                    "UpdateRollback");

                return new InstallationResult
                {
                    Success = false,
                    Message = "Cannot update the application. Please try again later.",
                    ErrorDetails = ex.Message,
                    InstalledVersion = currentBinaryVersion
                };
            }
        }

        /// <summary>
        /// Uninstall Application
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName)
        {
            try
            {
                Logger.Info("Starting uninstall for {AppCode}", appCode);

                var appFolder = Path.Combine(_appBasePath, appCode);
                if (Directory.Exists(appFolder))
                {
                    Directory.Delete(appFolder, true);
                    Logger.Info("Deleted application folder at {Path}", appFolder);
                }

                Logger.Info("Uninstall completed for {AppCode}", appCode);
                return new InstallationResult
                {
                    Success = true,
                    Message = "Uninstall completed successfully"
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Uninstall failed for {AppCode}", appCode);
                return new InstallationResult
                {
                    Success = false,
                    Message = "Uninstall failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Download and extract config to backup folder for verification
        /// </summary>
        public async Task<(bool Success, string? BackupConfigPath, string? ErrorMessage)> DownloadAndExtractConfigToBackupAsync(
            string appCode,
            string configPackage)
        {
            try
            {
                var configBackupPath = Path.Combine(_appBasePath, appCode, $"ConfigBackup_{DateTime.Now:yyyyMMddHHmmss}");
                Directory.CreateDirectory(configBackupPath);

                Logger.Info("Downloading config package {Package} to backup: {Path}", configPackage, configBackupPath);

                // Download config package from server
                var packageUrl = $"{_baseUrl}/api/apps/{appCode}/download/{configPackage}";
                var response = await _httpClient.GetAsync(packageUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.Error("Failed to download config package. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);
                    return (false, null, $"Failed to download config package. Status: {response.StatusCode}");
                }

                var packageData = await response.Content.ReadAsByteArrayAsync();

                if (packageData == null || packageData.Length == 0)
                {
                    return (false, null, $"Downloaded config package '{configPackage}' is empty");
                }

                Logger.Info("Downloaded {Size} bytes", packageData.Length);

                // Save to temp zip
                var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_config_{Guid.NewGuid()}.zip");
                await File.WriteAllBytesAsync(tempZip, packageData);

                try
                {
                    // Extract to backup folder
                    ZipFile.ExtractToDirectory(tempZip, configBackupPath, overwriteFiles: true);
                    Logger.Info("Config extracted to backup: {Path}", configBackupPath);

                    return (true, configBackupPath, null);
                }
                finally
                {
                    if (File.Exists(tempZip))
                    {
                        File.Delete(tempZip);
                        Logger.Debug("Deleted temp zip: {Path}", tempZip);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to download and extract config for {AppCode}", appCode);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Verify config files in backup folder (check if config files exist)
        /// </summary>
        public async Task<bool> VerifyConfigInBackupAsync(string? backupConfigPath)
        {
            await Task.Delay(100);

            if (string.IsNullOrEmpty(backupConfigPath) || !Directory.Exists(backupConfigPath))
            {
                Logger.Error("Config backup folder not found: {Path}", backupConfigPath ?? "NULL");
                return false;
            }

            // Check if there are any config files (.json, .xml, .config, etc.)
            var configExtensions = new[] { "*.json", "*.xml", "*.config", "*.txt", "*.ini" };
            var configFiles = configExtensions
                .SelectMany(ext => Directory.GetFiles(backupConfigPath, ext, SearchOption.AllDirectories))
                .ToList();

            if (!configFiles.Any())
            {
                Logger.Error("No config files found in backup: {Path}", backupConfigPath);

                // Log all files for debugging
                var allFiles = Directory.GetFiles(backupConfigPath, "*.*", SearchOption.AllDirectories);
                Logger.Error("Files in backup folder: {Files}", string.Join(", ", allFiles.Select(Path.GetFileName)));

                return false;
            }

            Logger.Info("✅ Found {Count} config file(s) in backup: {Files}",
                configFiles.Count,
                string.Join(", ", configFiles.Select(Path.GetFileName)));

            return true;
        }

        /// <summary>
        /// Commit config update:
        /// 1. Copy all files from backup → Config folder
        /// 2. Save config version in Config folder
        /// 3. Copy all config files from Config → App folder (overwrite)
        /// </summary>
        public async Task<bool> CommitConfigUpdateAsync(
            string appCode,
            string? backupConfigPath,
            string newConfigVersion)
        {
            try
            {
                if (string.IsNullOrEmpty(backupConfigPath) || !Directory.Exists(backupConfigPath))
                {
                    Logger.Error("Backup config path is invalid: {Path}", backupConfigPath ?? "NULL");
                    return false;
                }

                var configPath = Path.Combine(_appBasePath, appCode, "Config");
                var appPath = Path.Combine(_appBasePath, appCode, "App");

                if (!Directory.Exists(appPath))
                {
                    Logger.Error("App folder not found: {Path}", appPath);
                    return false;
                }

                Logger.Info("Committing config update from backup: {Backup}", backupConfigPath);

                // ============================================
                // STEP 1: Copy all files from backup → Config folder
                // ============================================
                if (Directory.Exists(configPath))
                {
                    Directory.Delete(configPath, true);
                    Logger.Info("Deleted old Config folder");
                }
                Directory.CreateDirectory(configPath);

                await CopyDirectoryAsync(backupConfigPath, configPath, overwrite: true);
                Logger.Info("✅ Copied all files from backup → Config folder");

                // ============================================
                // STEP 2: Save config version in Config folder
                // ============================================
                var versionFilePath = Path.Combine(configPath, "version.txt");
                await File.WriteAllTextAsync(versionFilePath, newConfigVersion);
                Logger.Info("✅ Saved config version: {Version} in Config folder", newConfigVersion);

                // ============================================
                // STEP 3: Copy all config files from Config → App folder (OVERWRITE)
                // ============================================
                Logger.Info("Copying config files from Config → App folder...");

                var configFiles = Directory.GetFiles(configPath, "*.*", SearchOption.AllDirectories);
                int copiedCount = 0;

                foreach (var configFile in configFiles)
                {
                    // Skip version file
                    if (Path.GetFileName(configFile) == "version.txt")
                        continue;

                    // Get relative path from Config folder
                    var relativePath = Path.GetRelativePath(configPath, configFile);
                    var destFile = Path.Combine(appPath, relativePath);

                    // Create subdirectory if needed
                    var destDir = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copy file (overwrite)
                    File.Copy(configFile, destFile, overwrite: true);
                    copiedCount++;
                    Logger.Debug("Copied: {File} → App folder", Path.GetFileName(configFile));
                }

                Logger.Info("✅ Copied {Count} config file(s) from Config → App folder (overwrite)", copiedCount);
                Logger.Info("✅ Config update committed successfully. Version: {Version}", newConfigVersion);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to commit config update for {AppCode}", appCode);
                return false;
            }
        }

        /// <summary>
        /// Helper method to copy directory recursively with overwrite option
        /// </summary>
        private async Task CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false)
        {
            Directory.CreateDirectory(destDir);

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }

            // Copy all subdirectories recursively
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = new DirectoryInfo(dir).Name;
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryAsync(dir, destSubDir, overwrite);
            }
        }

        /// <summary>
        /// Cleanup config backup folder
        /// </summary>
        public async Task CleanupConfigBackupAsync(string? backupConfigPath)
        {
            await Task.Delay(50);

            try
            {
                if (!string.IsNullOrEmpty(backupConfigPath) && Directory.Exists(backupConfigPath))
                {
                    Directory.Delete(backupConfigPath, true);
                    Logger.Info("🗑️ Cleaned up config backup: {Path}", backupConfigPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to cleanup config backup: {Path}", backupConfigPath);
            }
        }

        /// <summary>
        /// Cleanup all update folders (NewVersion_* and Backup_*) after successful launch
        /// </summary>
        public async Task CleanupUpdateFoldersAsync(string appCode)
        {
            await Task.Delay(50);

            try
            {
                var updatesPath = Path.Combine(_appBasePath, appCode, "Updates");

                if (!Directory.Exists(updatesPath))
                {
                    Logger.Debug("No Updates folder to cleanup for {AppCode}", appCode);
                    return;
                }

                // Get all NewVersion_* and Backup_* folders
                var foldersToDelete = Directory.GetDirectories(updatesPath)
                    .Where(dir =>
                    {
                        var folderName = new DirectoryInfo(dir).Name;
                        return folderName.StartsWith("NewVersion_") || folderName.StartsWith("Backup_");
                    })
                    .ToList();

                if (!foldersToDelete.Any())
                {
                    Logger.Debug("No update folders to cleanup for {AppCode}", appCode);
                    return;
                }

                Logger.Info("🗑️ Cleaning up {Count} update folder(s) for {AppCode}", foldersToDelete.Count, appCode);

                foreach (var folder in foldersToDelete)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        Logger.Info("✅ Deleted: {Folder}", new DirectoryInfo(folder).Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to delete folder: {Folder}", folder);
                    }
                }

                // If Updates folder is now empty, delete it too
                if (!Directory.EnumerateFileSystemEntries(updatesPath).Any())
                {
                    Directory.Delete(updatesPath);
                    Logger.Info("🗑️ Deleted empty Updates folder for {AppCode}", appCode);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to cleanup update folders for {AppCode}", appCode);
            }
        }

        #endregion

        #region Public Commit/Rollback Methods

        /// <summary>
        /// Commit update - Move from NewVersion to App + Save version/manifest
        /// DO NOT delete backup yet - that's done after verification in ViewModel
        /// </summary>
        public async Task<bool> CommitUpdateAsync(string appCode, ManifestDto manifest, string backupPath, string newVersionPath)
        {
            var stopwatch = Stopwatch.StartNew();
            string? oldVersion = null;

            try
            {
                oldVersion = GetVersionFromBackup(backupPath);

                Logger.Info("Committing update for {AppCode} - Old: {OldVersion},  {NewVersion}",
                    appCode, oldVersion ?? "N/A", manifest.Binary?.Version);

                // Validate inputs BEFORE doing anything destructive
                if (string.IsNullOrEmpty(newVersionPath) || !Directory.Exists(newVersionPath))
                {
                    Logger.Error("Cannot commit update: NewVersion path invalid - Path: {Path}", newVersionPath ?? "NULL");
                    throw new Exception($"Cannot commit update: NewVersion folder not found at {newVersionPath}");
                }

                // Verify NewVersion folder has content
                var newVersionFiles = Directory.GetFiles(newVersionPath, "*.*", SearchOption.AllDirectories);
                if (newVersionFiles.Length == 0)
                {
                    Logger.Error("NewVersion folder is empty: {Path}", newVersionPath);
                    throw new Exception($"NewVersion folder is empty: {newVersionPath}");
                }

                Logger.Info("NewVersion folder verified - contains {Count} files", newVersionFiles.Length);

                // Verify backup exists (critical for rollback)
                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                {
                    Logger.Error("Backup not found - cannot safely commit without rollback capability");
                    throw new Exception($"Backup folder not found at: {backupPath}");
                }

                var backupFiles = Directory.GetFiles(Path.Combine(backupPath, "App"), "*.*", SearchOption.AllDirectories);
                Logger.Info("Backup verified at: {BackupPath} - contains {Count} files", backupPath, backupFiles.Length);

                // 🔥 CRITICAL SECTION: Move new version
                var appPath = Path.Combine(_appBasePath, appCode, "App");

                Logger.Info("Starting critical section: Moving NewVersion to App");

                // Delete old App folder
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                    Logger.Info("Deleted old App folder");
                }

                // Move new version folder to App
                Directory.Move(newVersionPath, appPath);
                Logger.Info("Moved NewVersion to App folder");

                // Verify move was successful
                var movedFiles = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                Logger.Info("Binary moved - App folder now contains {Count} files", movedFiles.Length);

                // 2. Save version.txt (both binary and config)
                SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");

                // 3. Save manifest.json
                await _manifestService.SaveManifestAsync(appCode, manifest);

                // 4. Clear failed update marker
                await ClearUpdateFailureMarkerAsync(appCode);

                stopwatch.Stop();

                // 5. Notify server
                //await NotifyInstallationAsync(
                //    appCode,
                //    manifest.Binary?.Version ?? "0.0.0",
                //    true,
                //    stopwatch.Elapsed,
                //    null,
                //    oldVersion,
                //    "Update"
                //);

                // ⚠️ IMPORTANT: DO NOT delete backup here!
                // Backup will be deleted AFTER verification in ViewModel
                Logger.Info("Commit completed - backup preserved for post-commit verification");
                Logger.Info("Backup location: {BackupPath}", backupPath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ Failed to commit update for {AppCode}", appCode);

                stopwatch.Stop();

                await NotifyInstallationAsync(
                    appCode,
                    manifest.Binary?.Version ?? "0.0.0",
                    false,
                    stopwatch.Elapsed,
                    $"Commit failed: {ex.Message}",
                    oldVersion,
                    "UpdateCommitFailed"
                );

                // DON'T delete backup - needed for rollback!
                Logger.Warn("⚠️ Keeping backup for rollback. Backup: {Backup}", backupPath);

                return false; // Rollback will be triggered in ViewModel
            }
        }

        /// <summary>
        /// Rollback update if verification or commit failed
        /// </summary>
        /// <summary>
        /// Rollback update if verification or commit failed
        /// </summary>
        public async Task<bool> RollbackUpdateAsync(string appCode, string backupPath, string failedVersion)
        {
            var stopwatch = Stopwatch.StartNew();
            string? restoredVersion = null;

            try
            {
                Logger.Warn("⚠️ ===== STARTING ROLLBACK for {AppCode} =====", appCode);
                Logger.Warn("Failed version: {Version}, Backup: {Backup}", failedVersion, backupPath);

                // Validate backup exists
                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                {
                    Logger.Error("❌ CRITICAL: Backup not found at {BackupPath}", backupPath ?? "NULL");
                    throw new Exception($"Backup not found at: {backupPath}");
                }

                var backupAppPath = Path.Combine(backupPath, "App");
                if (!Directory.Exists(backupAppPath))
                {
                    Logger.Error("❌ CRITICAL: Backup App folder not found at {Path}", backupAppPath);
                    throw new Exception($"Backup App folder not found at: {backupAppPath}");
                }

                var backupFiles = Directory.GetFiles(backupAppPath, "*.*", SearchOption.AllDirectories);
                Logger.Info("Backup contains {Count} files", backupFiles.Length);

                // 1. Perform rollback (restore from backup)
                await PerformRollbackAsync(appCode, backupPath);

                // 2. Get restored version
                restoredVersion = GetCurrentVersion(appCode, "App");
                Logger.Info("Restored to version: {Version}", restoredVersion ?? "unknown");

                // 3. Verify restore was successful
                var appPath = Path.Combine(_appBasePath, appCode, "App");
                if (!Directory.Exists(appPath))
                {
                    throw new Exception("Restore failed - App folder still missing after rollback");
                }

                var restoredFiles = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                Logger.Info("Restored App folder contains {Count} files", restoredFiles.Length);

                // 4. Mark update as failed (to prevent retry loops)
                await MarkUpdateAsFailedAsync(appCode, failedVersion);

                stopwatch.Stop();

                // 5. Notify server
                await NotifyInstallationAsync(
                    appCode,
                    restoredVersion ?? "unknown",
                    true,
                    stopwatch.Elapsed,
                    $"Update to {failedVersion} failed commit. System rolled back to {restoredVersion}",
                    restoredVersion,
                    "UpdateRollback"
                );

                // 6. ONLY delete backup and cleanup after successful rollback
                DeleteBackupSafely(backupPath);
                CleanupUpdateFolders(appCode);

                Logger.Info("===== ROLLBACK COMPLETED for {AppCode} to version {Version} =====",
                    appCode, restoredVersion);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ CRITICAL: Rollback failed for {AppCode}", appCode);

                stopwatch.Stop();

                await NotifyInstallationAsync(
                    appCode,
                    restoredVersion ?? "unknown",
                    false,
                    stopwatch.Elapsed,
                    $"CRITICAL: Rollback failed: {ex.Message}. Backup location: {backupPath}",
                    null,
                    "RollbackFailed"
                );

                // Don't delete backup - user might need it for manual recovery
                Logger.Error("⚠️ Backup preserved at: {Backup} for manual recovery", backupPath);

                return false;
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Download and extract package from server
        /// </summary>
        private async Task DownloadAndExtractAsync(string appCode, string packageName, string targetPath)
        {
            var packageUrl = $"/api/apps/{appCode}/download/{packageName}";
            var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_{Guid.NewGuid()}.zip");

            try
            {
                Logger.Info("Downloading package from {Url} to {Path}", packageUrl, targetPath);

                var response = await _httpClient.GetAsync(packageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to download package: {response.StatusCode}");
                }

                var packageData = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempZip, packageData);

                Logger.Info("Downloaded {Size} bytes, extracting...", packageData.Length);

                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                Directory.CreateDirectory(targetPath);

                ZipFile.ExtractToDirectory(tempZip, targetPath, overwriteFiles: true);

                Logger.Info("Successfully extracted to {Path}", targetPath);
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }
            }
        }

        /// <summary>
        /// Download new version to dedicated folder (not system temp)
        /// </summary>
        private async Task DownloadNewVersionAsync(string appCode, ManifestDto manifest, string updateType, string targetPath)
        {
            try
            {
                // Create and verify target directory
                Directory.CreateDirectory(targetPath);
                Logger.Info("Created new version directory: {Path}", targetPath);

                // ALWAYS download binary for version updates
                if (string.IsNullOrEmpty(manifest.Binary?.Package))
                {
                    throw new Exception("Binary package not specified in manifest");
                }

                Logger.Info("Downloading binary package to: {Path}", targetPath);
                await DownloadAndExtractAsync(appCode, manifest.Binary.Package, targetPath);

                // Verify extraction
                var files = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories);
                Logger.Info("Extracted {Count} files. Sample: {Sample}",
                    files.Length,
                    string.Join(", ", files.Take(5).Select(Path.GetFileName)));

                if (files.Length == 0)
                {
                    throw new Exception("No files extracted from binary package");
                }

                // Update CONFIG (SELECTIVE) - Optional
                if (!string.IsNullOrEmpty(manifest.Config?.Package))
                {
                    Logger.Info("Updating config with strategy: {Strategy}", manifest.Config.MergeStrategy);

                    var tempConfigZip = Path.Combine(Path.GetTempPath(), $"{appCode}_config_{Guid.NewGuid()}.zip");
                    var configUrl = $"{_baseUrl}/api/apps/{appCode}/download/{manifest.Config.Package}";

                    var response = await _httpClient.GetAsync(configUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var configData = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(tempConfigZip, configData);

                        await _selectiveUpdateService.ApplySelectiveConfigUpdateAsync(
                            appCode,
                            manifest,
                            tempConfigZip
                        );

                        if (File.Exists(tempConfigZip))
                        {
                            File.Delete(tempConfigZip);
                        }

                        Logger.Info("Config update completed successfully");
                    }
                    else
                    {
                        Logger.Warn("Failed to download config package: {StatusCode}", response.StatusCode);
                    }
                }

                // Final verification
                if (!Directory.Exists(targetPath))
                {
                    throw new Exception($"Target folder disappeared: {targetPath}");
                }

                var finalFiles = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories);
                if (finalFiles.Length == 0)
                {
                    throw new Exception("Target folder is empty after download");
                }

                Logger.Info("Download completed successfully to: {Path}", targetPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to download new version");
                DeleteFolderSafely(targetPath, "new version");
                throw;
            }
        }

        /// <summary>
        /// Move new version from NewVersion folder to App folder
        /// </summary>
        private void MoveNewVersionToAppFolder(string appCode, string newVersionPath)
        {
            var appPath = Path.Combine(_appBasePath, appCode, "App");

            try
            {
                Logger.Info("Moving new version from {NewVersionPath} to {AppPath}", newVersionPath, appPath);

                // Verify source folder exists and has content
                if (!Directory.Exists(newVersionPath))
                {
                    throw new Exception($"New version folder not found: {newVersionPath}");
                }

                var sourceFiles = Directory.GetFiles(newVersionPath, "*.*", SearchOption.AllDirectories);
                if (sourceFiles.Length == 0)
                {
                    throw new Exception($"New version folder is empty: {newVersionPath}");
                }

                Logger.Info("Source folder contains {Count} files", sourceFiles.Length);

                // Delete old App folder
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                    Logger.Info("Deleted old App folder");
                }

                // Move new version folder to App
                Directory.Move(newVersionPath, appPath);

                // Verify move was successful
                var movedFiles = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                Logger.Info("New version moved successfully - App folder now contains {Count} files", movedFiles.Length);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to move new version from {Source} to {Dest}", newVersionPath, appPath);
                throw;
            }
        }

        /// <summary>
        /// Save version information
        /// </summary>
        private void SaveVersionInfo(string appCode, string binaryVersion, string configVersion)
        {
            if (!string.IsNullOrEmpty(binaryVersion))
            {
                var versionFile = Path.Combine(_appBasePath, appCode, "App", "version.txt");
                var versionDir = Path.GetDirectoryName(versionFile);

                if (!string.IsNullOrEmpty(versionDir) && !Directory.Exists(versionDir))
                {
                    Directory.CreateDirectory(versionDir);
                }

                File.WriteAllText(versionFile, binaryVersion);
                Logger.Info("Saved binary version {Version} to {Path}", binaryVersion, versionFile);
            }

            if (!string.IsNullOrEmpty(configVersion))
            {
                var configVersionFile = Path.Combine(_appBasePath, appCode, "Config", "version.txt");
                var versionDir = Path.GetDirectoryName(configVersionFile);

                if (!string.IsNullOrEmpty(versionDir) && !Directory.Exists(versionDir))
                {
                    Directory.CreateDirectory(versionDir);
                }

                File.WriteAllText(configVersionFile, configVersion);
                Logger.Info("Saved config version {Version} to {Path}", configVersion, configVersionFile);
            }
        }

        /// <summary>
        /// Notify server
        /// </summary>
        public async Task NotifyInstallationAsync(
            string appCode,
            string version,
            bool success,
            TimeSpan duration,
            string? error = null,
            string? oldVersion = null,
            string action = "Install")
        {
            try
            {
                var logUrl = $"{_baseUrl}/api/Installation/log";
                var logData = new
                {
                    appCode,
                    version,
                    oldVersion,
                    action,
                    userName = Environment.UserName,
                    machineName = Environment.MachineName,
                    success,
                    error,
                    durationSeconds = (int)duration.TotalSeconds,
                    timestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(logData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(logUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Info("Installation logged to server successfully - Action: {Action}, Success: {Success}", action, success);
                }
                else
                {
                    Logger.Warn("Failed to log installation: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to notify server about installation (non-critical)");
            }
        }

        /// <summary>
        /// GetCurrentVersion
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        private string? GetCurrentVersion(string appCode, string folder)
        {
            try
            {
                var versionFile = Path.Combine(_appBasePath, appCode, folder, "version.txt");
                if (File.Exists(versionFile))
                {
                    return File.ReadAllText(versionFile).Trim();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to read current version for {AppCode}/{Folder}", appCode, folder);
            }
            return null;
        }

        /// <summary>
        /// GetVersionFromBackup
        /// </summary>
        /// <param name="backupPath"></param>
        /// <returns></returns>
        public string? GetVersionFromBackup(string backupPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
                {
                    var backupVersionFile = Path.Combine(backupPath, "App", "version.txt");
                    if (File.Exists(backupVersionFile))
                    {
                        return File.ReadAllText(backupVersionFile).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to read version from backup");
            }
            return null;
        }

        /// <summary>
        /// CreateBackupAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="backupPath"></param>
        /// <returns></returns>
        private async Task CreateBackupAsync(string appCode, string backupPath)
        {
            var appPath = Path.Combine(_appBasePath, appCode, "App");
            var manifestPath = Path.Combine(_appBasePath, appCode, "manifest.json");

            if (Directory.Exists(appPath))
            {
                CopyDirectory(appPath, Path.Combine(backupPath, "App"));
                Logger.Info("Created backup of App folder at {BackupPath}", backupPath);
            }

            if (File.Exists(manifestPath))
            {
                var backupManifestPath = Path.Combine(backupPath, "manifest.json");
                Directory.CreateDirectory(backupPath);
                File.Copy(manifestPath, backupManifestPath, true);
                Logger.Info("Backed up manifest.json");
            }
        }

        /// <summary>
        /// PerformRollbackAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="backupPath"></param>
        /// <returns></returns>
        private async Task PerformRollbackAsync(string appCode, string backupPath)
        {
            try
            {
                Logger.Info("⚠️ Executing rollback restore for {AppCode} from {BackupPath}", appCode, backupPath);

                if (!Directory.Exists(backupPath))
                {
                    Logger.Error("Backup not found at {BackupPath}", backupPath);
                    throw new Exception($"Backup not found at: {backupPath}");
                }

                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var backupAppPath = Path.Combine(backupPath, "App");

                if (!Directory.Exists(backupAppPath))
                {
                    throw new Exception($"Backup App folder not found at: {backupAppPath}");
                }

                // 1. Delete corrupted/incomplete App folder if exists
                if (Directory.Exists(appPath))
                {
                    try
                    {
                        Directory.Delete(appPath, true);
                        Logger.Info("Deleted corrupted App folder at {Path}", appPath);
                    }
                    catch (Exception delEx)
                    {
                        Logger.Warn(delEx, "Failed to delete App folder, attempting to overwrite");
                        // Continue - CopyDirectory might still work
                    }
                }

                // 2. Restore App folder from backup (includes version.txt)
                CopyDirectory(backupAppPath, appPath);
                Logger.Info("Restored App folder from backup");

                // Verify restore
                var restoredFiles = Directory.GetFiles(appPath, "*.*", SearchOption.AllDirectories);
                if (restoredFiles.Length == 0)
                {
                    throw new Exception("Restore verification failed - App folder is empty");
                }

                // 3. Restore manifest.json from backup
                var backupManifestPath = Path.Combine(backupPath, "manifest.json");
                var manifestPath = Path.Combine(_appBasePath, appCode, "manifest.json");

                if (File.Exists(backupManifestPath))
                {
                    File.Copy(backupManifestPath, manifestPath, true);
                    Logger.Info("Restored manifest.json from backup");
                }

                // 4. Verify rollback
                var restoredVersion = GetCurrentVersion(appCode, "App");
                Logger.Info("Rollback verification passed - version: {Version}", restoredVersion ?? "unknown");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "❌ PerformRollbackAsync failed for {AppCode}", appCode);
                throw; // Re-throw to let caller handle
            }
        }
        /// <summary>
        /// Copies directory recursively
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        /// <summary>
        /// DeleteBackupSafely
        /// </summary>
        /// <param name="backupPath"></param>
        private void DeleteBackupSafely(string? backupPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(backupPath) && Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                    Logger.Info("Deleted backup: {BackupPath}", backupPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to delete backup (non-critical): {BackupPath}", backupPath);
            }
        }

        /// <summary>
        /// Generic folder deletion with logging
        /// </summary>
        private void DeleteFolderSafely(string? folderPath, string folderType)
        {
            try
            {
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    Logger.Info("Deleted {Type} folder: {Path}", folderType, folderPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to delete {Type} folder (non-critical): {Path}", folderType, folderPath);
            }
        }

        /// <summary>
        /// Cleanup old update folders (Backup_*, NewVersion_*)
        /// </summary>
        private void CleanupUpdateFolders(string appCode)
        {
            try
            {
                var updateBasePath = Path.Combine(_appBasePath, appCode, "Updates");
                if (!Directory.Exists(updateBasePath))
                {
                    return;
                }

                var oldFolders = Directory.GetDirectories(updateBasePath)
                    .Where(d => Path.GetFileName(d).StartsWith("Backup_") ||
                                Path.GetFileName(d).StartsWith("NewVersion_"))
                    .ToList();

                foreach (var folder in oldFolders)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        Logger.Info("Cleaned up old update folder: {Folder}", folder);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed to delete update folder: {Folder}", folder);
                    }
                }

                // Remove Updates folder if empty
                if (!Directory.EnumerateFileSystemEntries(updateBasePath).Any())
                {
                    Directory.Delete(updateBasePath);
                    Logger.Info("Removed empty Updates folder");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to cleanup update folders (non-critical)");
            }
        }

        /// <summary>
        /// MarkUpdateAsFailedAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="failedVersion"></param>
        /// <returns></returns>
        private async Task MarkUpdateAsFailedAsync(string appCode, string failedVersion)
        {
            try
            {
                var failureMarkerPath = Path.Combine(_appBasePath, appCode, ".update_failed");
                var failureData = new
                {
                    FailedVersion = failedVersion,
                    Timestamp = DateTime.UtcNow,
                    MachineName = Environment.MachineName,
                    ErrorType = "UpdateFailed_AutoRollback"
                };

                await File.WriteAllTextAsync(failureMarkerPath, JsonSerializer.Serialize(failureData));
                Logger.Info("Marked update as failed for version {Version}", failedVersion);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to mark update as failed (non-critical)");
            }
        }

        /// <summary>
        /// Checks if an update has previously failed
        /// </summary>
        private async Task<bool> HasUpdateFailedBeforeAsync(string appCode)
        {
            try
            {
                var failureMarkerPath = Path.Combine(_appBasePath, appCode, ".update_failed");
                if (File.Exists(failureMarkerPath))
                {
                    var content = await File.ReadAllTextAsync(failureMarkerPath);
                    Logger.Info("Found previous update failure marker: {Data}", content);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to check update failure marker (non-critical)");
            }
            return false;
        }

        /// <summary>
        /// Clears the update failure marker after successful update
        /// </summary>
        private async Task ClearUpdateFailureMarkerAsync(string appCode)
        {
            try
            {
                var failureMarkerPath = Path.Combine(_appBasePath, appCode, ".update_failed");
                if (File.Exists(failureMarkerPath))
                {
                    File.Delete(failureMarkerPath);
                    Logger.Info("Cleared update failure marker");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to clear update failure marker (non-critical)");
            }
        }

        /// <summary>
        /// Finalize update after successful verification - Delete backup and cleanup
        /// </summary>
        public async Task<bool> FinalizeUpdateAsync(string appCode, string backupPath)
        {
            try
            {
                Logger.Info("Finalizing update for {AppCode} - deleting backup", appCode);

                // Delete backup
                DeleteBackupSafely(backupPath);

                // Cleanup old update folders
                CleanupUpdateFolders(appCode);

                Logger.Info("Update finalized successfully for {AppCode}", appCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to finalize update (non-critical)");
                return false;
            }
        }
        #endregion
    }
}