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
            _appBasePath = @"C:\CompanyApps";
            _manifestService = new ManifestService();
            _selectiveUpdateService = new SelectiveUpdateService(_manifestService);

            if (!Directory.Exists(_appBasePath))
            {
                Directory.CreateDirectory(_appBasePath);
            }

            Logger.Debug("InstallationService initialized with base path: {Path}", _appBasePath);
        }

        #region Public Interface Methods

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

                await NotifyInstallationAsync(
                    appCode,
                    result.InstalledVersion ?? string.Empty,
                    true,
                    stopwatch.Elapsed,
                    null,
                    null,
                    "Install");

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

        public async Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName)
        {
            var stopwatch = Stopwatch.StartNew();
            string? currentBinaryVersion = null;
            string? newVersion = null;
            string? backupPath = null;
            string? tempAppPath = null;

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

                backupPath = Path.Combine(_appBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");

                // Backup current installation
                await CreateBackupAsync(appCode, backupPath);

                try
                {
                    // ✅ CRITICAL: Download to TEMP folder, don't replace App folder yet
                    tempAppPath = await DownloadUpdatePackagesToTempAsync(appCode, manifest, updateType);

                    Logger.Info("Package downloaded to temp: {TempPath}", tempAppPath);

                    stopwatch.Stop();

                    // ✅ Return temp path to ViewModel for verification
                    return new InstallationResult
                    {
                        Success = true,
                        Message = $"Downloaded version {newVersion}. Checking stability...",
                        InstalledVersion = currentBinaryVersion,
                        UpdatedManifest = manifest,
                        BackupPath = backupPath,
                        TempAppPath = tempAppPath
                    };
                }
                catch (Exception updateEx)
                {
                    Logger.Error(updateEx, "Update failed during download for {AppCode}", appCode);

                    // Clean up temp folder
                    DeleteTempFolderSafely(tempAppPath);

                    stopwatch.Stop();

                    await NotifyInstallationAsync(
                        appCode,
                        currentBinaryVersion ?? "unknown",
                        false,
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
                    false,
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

        #endregion

        #region Public Commit/Rollback Methods

        /// <summary>
        /// Commit update - Move from TEMP to App + Save version/manifest
        /// </summary>
        public async Task<bool> CommitUpdateAsync(string appCode, ManifestDto manifest, string backupPath, string tempAppPath)
        {
            var stopwatch = Stopwatch.StartNew();
            string? oldVersion = null;

            try
            {
                oldVersion = GetVersionFromBackup(backupPath);

                Logger.Info("Committing update for {AppCode} - Old: {OldVersion}, New: {NewVersion}",
                    appCode, oldVersion ?? "N/A", manifest.Binary?.Version);

                // 1. Move new version from TEMP to App folder
                MoveNewVersionToAppFolder(appCode, tempAppPath);

                // 2. Save version.txt
                SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");

                // 3. Save manifest.json
                await _manifestService.SaveManifestAsync(appCode, manifest);

                // 4. Clear failed update marker
                await ClearUpdateFailureMarkerAsync(appCode);

                stopwatch.Stop();

                // 5. Notify server
                await NotifyInstallationAsync(
                    appCode,
                    manifest.Binary?.Version ?? "0.0.0",
                    true,
                    stopwatch.Elapsed,
                    null,
                    oldVersion,
                    "Update"
                );

                // 6. Delete backup
                DeleteBackupSafely(backupPath);

                Logger.Info("Update committed successfully for {AppCode}", appCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to commit update for {AppCode}", appCode);

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

                return false;
            }
        }

        /// <summary>
        /// Rollback update if verification failed
        /// </summary>
        public async Task<bool> RollbackUpdateAsync(string appCode, string backupPath, string failedVersion)
        {
            var stopwatch = Stopwatch.StartNew();
            string? restoredVersion = null;

            try
            {
                Logger.Warn("Rolling back update for {AppCode} - Failed version: {Version}", appCode, failedVersion);

                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                {
                    Logger.Error("Backup not found at {BackupPath}, cannot rollback", backupPath);
                    return false;
                }

                // 1. Perform rollback
                await PerformRollbackAsync(appCode, backupPath);

                // 2. Get restored version
                restoredVersion = GetCurrentVersion(appCode, "App");
                Logger.Info("Restored to version: {Version}", restoredVersion ?? "unknown");

                // 3. Mark update as failed
                await MarkUpdateAsFailedAsync(appCode, failedVersion);

                stopwatch.Stop();

                // 4. Notify server
                await NotifyInstallationAsync(
                    appCode,
                    restoredVersion ?? "unknown",
                    false,
                    stopwatch.Elapsed,
                    $"Update to {failedVersion} failed verification. Executable not found or corrupted. System rolled back to {restoredVersion}",
                    restoredVersion,
                    "UpdateRollback"
                );

                // 5. Delete backup
                DeleteBackupSafely(backupPath);

                Logger.Info("Rollback completed successfully for {AppCode}", appCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Rollback failed for {AppCode}", appCode);

                stopwatch.Stop();

                await NotifyInstallationAsync(
                    appCode,
                    restoredVersion ?? "unknown",
                    false,
                    stopwatch.Elapsed,
                    $"Rollback failed: {ex.Message}",
                    null,
                    "RollbackFailed"
                );

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
        /// Download update packages to TEMP folder
        /// </summary>
        private async Task<string> DownloadUpdatePackagesToTempAsync(string appCode, ManifestDto manifest, string updateType)
        {
            // Create TEMP folder
            var tempAppPath = Path.Combine(Path.GetTempPath(), $"{appCode}_NewVersion_{Guid.NewGuid()}");

            try
            {
                // Update BINARY to TEMP
                if (updateType == "binary" || updateType == "both")
                {
                    Logger.Info("Downloading binary package to temp folder");
                    await DownloadAndExtractAsync(appCode, manifest.Binary?.Package!, tempAppPath);
                    Logger.Info("Binary extracted to temp: {TempPath}", tempAppPath);
                }

                // Update CONFIG (SELECTIVE)
                if ((updateType == "config" || updateType == "both") &&
                    !string.IsNullOrEmpty(manifest.Config?.Package))
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
                }

                return tempAppPath;
            }
            catch (Exception ex)
            {
                // Clean up temp folder on error
                DeleteTempFolderSafely(tempAppPath);
                throw;
            }
        }

        /// <summary>
        /// Move new version from TEMP to App folder
        /// </summary>
        private void MoveNewVersionToAppFolder(string appCode, string tempAppPath)
        {
            var appPath = Path.Combine(_appBasePath, appCode, "App");

            try
            {
                Logger.Info("Moving new version from {TempPath} to {AppPath}", tempAppPath, appPath);

                // Delete old App folder
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                    Logger.Info("Deleted old App folder");
                }

                // Move temp folder to App
                Directory.Move(tempAppPath, appPath);

                Logger.Info("New version moved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to move new version");
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
        private async Task NotifyInstallationAsync(
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

        private string? GetVersionFromBackup(string backupPath)
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

        private async Task PerformRollbackAsync(string appCode, string backupPath)
        {
            try
            {
                Logger.Info("Starting rollback for {AppCode} from backup {BackupPath}", appCode, backupPath);

                if (!Directory.Exists(backupPath))
                {
                    Logger.Error("Backup not found at {BackupPath}, cannot rollback", backupPath);
                    throw new Exception("Backup not found, cannot perform rollback");
                }

                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var backupAppPath = Path.Combine(backupPath, "App");

                // 1. Delete failed update App folder
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                    Logger.Info("Deleted failed update at {Path}", appPath);
                }

                // 2. Restore App folder from backup (includes version.txt)
                if (Directory.Exists(backupAppPath))
                {
                    CopyDirectory(backupAppPath, appPath);
                    Logger.Info("Restored App folder from backup");
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
                Logger.Info("Rollback completed successfully to version {Version}", restoredVersion ?? "unknown");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Rollback failed for {AppCode}", appCode);
                throw;
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
        /// Delete temp folder safely
        /// </summary>
        private void DeleteTempFolderSafely(string? tempPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(tempPath) && Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                    Logger.Info("Deleted temp folder: {TempPath}", tempPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to delete temp folder (non-critical): {TempPath}", tempPath);
            }
        }

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

        #endregion
    }
}