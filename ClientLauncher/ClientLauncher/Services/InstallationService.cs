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

            // Base path at C:\CompanyApps
            _appBasePath = @"C:\CompanyApps";
            _manifestService = new ManifestService();
            _selectiveUpdateService = new SelectiveUpdateService(_manifestService);

            if (!Directory.Exists(_appBasePath))
            {
                Directory.CreateDirectory(_appBasePath);
            }

            Logger.Debug("InstallationService initialized with base path: {Path}", _appBasePath);
        }

        public async Task<InstallationResult> InstallApplicationAsync(string appCode, string userName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Logger.Info("Starting installation for {AppCode}", appCode);
                // 1. Get manifest from server (database-generated)
                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Failed to retrieve manifest from server");
                }

                // C:\CompanyApps\{appCode}\App
                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var binaryPackage = manifest.Binary?.Package;

                if (string.IsNullOrEmpty(binaryPackage))
                {
                    throw new Exception("Invalid manifest: binary package not specified");
                }

                // 2. Download and extract binary package
                Logger.Info("Downloading binary package: {Package}", binaryPackage);
                await DownloadAndExtractAsync(appCode, binaryPackage, appPath);

                // 3. Download config if specified
                if (!string.IsNullOrEmpty(manifest.Config?.Package) &&
                    manifest.UpdatePolicy?.Type != "binary")
                {
                    var configPath = Path.Combine(_appBasePath, appCode, "Config");
                    Logger.Info("Downloading config package: {Package}", manifest.Config.Package);
                    await DownloadAndExtractAsync(appCode, manifest.Config.Package, configPath);
                }

                // 4. Save version info to C:\CompanyApps\{appCode}\version.txt
                SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");

                // Save manifest to C:\CompanyApps\{appCode}\manifest.json
                await _manifestService.SaveManifestAsync(appCode, manifest);

                Logger.Info("Installation completed for {AppCode}", appCode);
                stopwatch.Stop();

                var result = new InstallationResult();
                result.Success = true;
                result.InstalledVersion = manifest.Binary?.Version;
                result.Message = $"Installation completed successfully in {stopwatch.Elapsed.TotalSeconds:F2}s";
                result.InstallationPath = appPath;

                await NotifyInstallationAsync(appCode, result?.InstalledVersion ?? string.Empty, true, stopwatch.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await NotifyInstallationAsync(appCode, "0.0.0", false, stopwatch.Elapsed, ex.Message);
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
            string? currentConfigVersion = null;
            string? newVersion = null;

            try
            {
                Logger.Info("Starting update for {AppCode}", appCode);

                // Get current version before update
                currentBinaryVersion = GetCurrentVersion(appCode, "App");
                currentConfigVersion = GetCurrentVersion(appCode, "Config");
                Logger.Info("Current version - Binary: {BinaryVersion}, Config: {ConfigVersion}",
                    currentBinaryVersion ?? "N/A", currentConfigVersion ?? "N/A");

                // Check if this version has failed before
                if (await HasUpdateFailedBeforeAsync(appCode))
                {
                    Logger.Warn("Update was previously attempted and failed for {AppCode}", appCode);
                    return new InstallationResult
                    {
                        Success = false,
                        Message = "Bản cập nhật mới đang có lỗi. Hệ thống đã tự động rollback về phiên bản cũ. Vui lòng thử lại sau.",
                        ErrorDetails = "Previous update attempt failed, automatic rollback applied",
                        InstalledVersion = currentBinaryVersion
                    };
                }

                // 1. Get latest manifest from server
                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Failed to retrieve manifest from server");
                }

                newVersion = manifest.Binary?.Version;
                var updateType = manifest.UpdatePolicy?.Type ?? "both";
                Logger.Info("Update type: {UpdateType}, New version: {NewVersion}", updateType, newVersion);

                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var configPath = Path.Combine(_appBasePath, appCode, "Config");
                var backupPath = Path.Combine(_appBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");

                // 2. Backup current installation
                if (Directory.Exists(appPath))
                {
                    CopyDirectory(appPath, backupPath);
                    Logger.Info("Created backup at {BackupPath}", backupPath);
                }

                try
                {
                    // === UPDATE BINARY ===
                    if (updateType == "binary" || updateType == "both")
                    {
                        Logger.Info("Updating binary package");
                        await DownloadAndExtractAsync(appCode, manifest.Binary?.Package!, appPath);
                        SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");
                    }

                    // === UPDATE CONFIG (SELECTIVE) ===
                    if ((updateType == "config" || updateType == "both") &&
                        !string.IsNullOrEmpty(manifest.Config?.Package))
                    {
                        Logger.Info("Updating config with strategy: {Strategy}", manifest.Config.MergeStrategy);

                        // Download config package to temp location
                        var tempConfigZip = Path.Combine(Path.GetTempPath(), $"{appCode}_config_{Guid.NewGuid()}.zip");
                        var configUrl = $"{_baseUrl}/api/apps/{appCode}/download/{manifest.Config.Package}";

                        var response = await _httpClient.GetAsync(configUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var configData = await response.Content.ReadAsByteArrayAsync();
                            await File.WriteAllBytesAsync(tempConfigZip, configData);

                            // Apply selective update
                            await _selectiveUpdateService.ApplySelectiveConfigUpdateAsync(
                                appCode,
                                manifest,
                                tempConfigZip
                            );

                            // Clean up temp file
                            if (File.Exists(tempConfigZip))
                            {
                                File.Delete(tempConfigZip);
                            }

                            Logger.Info("Config update completed successfully");
                        }
                    }

                    // Update version info
                    SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");

                    // Update local manifest
                    await _manifestService.SaveManifestAsync(appCode, manifest);

                    // Clear failed update marker on success
                    await ClearUpdateFailureMarkerAsync(appCode);

                    // Delete backup on success
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                        Logger.Info("Deleted backup after successful update");
                    }

                    stopwatch.Stop();
                    await NotifyInstallationAsync(appCode, manifest.Binary?.Version ?? string.Empty,
                        true, stopwatch.Elapsed, null, currentBinaryVersion, "Update");

                    Logger.Info("Update completed successfully for {AppCode}", appCode);
                    return new InstallationResult
                    {
                        Success = true,
                        Message = $"Update successful from version {currentBinaryVersion} to {newVersion}",
                        InstalledVersion = manifest.Binary?.Version
                    };
                }
                catch (Exception updateEx)
                {
                    Logger.Error(updateEx, "Update failed, attempting rollback for {AppCode}", appCode);

                    // Rollback on error
                    await PerformRollbackAsync(appCode, appPath, backupPath, currentBinaryVersion, currentConfigVersion);

                    // Mark this update as failed
                    await MarkUpdateAsFailedAsync(appCode, newVersion ?? "unknown");

                    stopwatch.Stop();
                    await NotifyInstallationAsync(appCode, newVersion ?? string.Empty,
                        false, stopwatch.Elapsed, $"Update failed: {updateEx.Message}. Rolled back to version {currentBinaryVersion}",
                        currentBinaryVersion, "Update");

                    return new InstallationResult
                    {
                        Success = false,
                        Message = $"New version is ({newVersion}) has an error. The system has automatically rolled back to the previous version ({currentBinaryVersion}). Please try again later.",
                        ErrorDetails = updateEx.Message,
                        InstalledVersion = currentBinaryVersion
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update failed for {AppCode}", appCode);
                stopwatch.Stop();
                await NotifyInstallationAsync(appCode, newVersion ?? string.Empty, false, stopwatch.Elapsed,
                    ex.Message, currentBinaryVersion, "Update");

                return new InstallationResult
                {
                    Success = false,
                    Message = "Cannot update application. Please try again later.",
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

        private void SaveVersionInfo(string appCode, string binaryVersion, string configVersion)
        {
            if (!string.IsNullOrEmpty(binaryVersion))
            {
                var versionFile = Path.Combine(_appBasePath, $"{appCode}/App", "version.txt");
                var versionDir = Path.GetDirectoryName(versionFile);

                if (!string.IsNullOrEmpty(versionDir) && !Directory.Exists(versionDir))
                {
                    Directory.CreateDirectory(versionDir);
                }

                File.WriteAllText(versionFile, binaryVersion);
                Logger.Info("Saved version {Version} to {Path}", binaryVersion, versionFile);
            }

            if (!string.IsNullOrEmpty(configVersion))
            {
                var configVersionFile = Path.Combine(_appBasePath, $"{appCode}/Config", "version.txt");
                var versionDir = Path.GetDirectoryName(configVersionFile);

                if (!string.IsNullOrEmpty(versionDir) && !Directory.Exists(versionDir))
                {
                    Directory.CreateDirectory(versionDir);
                }

                File.WriteAllText(configVersionFile, configVersion);
                Logger.Info("Saved version {Version} to {Path}", configVersion, configVersionFile);
            }

        }

        private async Task NotifyInstallationAsync(string appCode, string version, bool success, TimeSpan duration, string? error = null, string? oldVersion = null, string action = "Install")
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
                    Logger.Info("Installation logged to server successfully");
                }
                else
                {
                    Logger.Warn($"Failed to log installation: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to notify server about installation (non-critical)");
            }
        }

        #region Rollback and Error Tracking Methods

        /// <summary>
        /// Gets the current installed version from version.txt
        /// </summary>
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
        /// Performs rollback by restoring from backup
        /// </summary>
        private async Task PerformRollbackAsync(string appCode, string appPath, string backupPath,
            string? binaryVersion, string? configVersion)
        {
            try
            {
                Logger.Info("Starting rollback for {AppCode}", appCode);

                // Delete failed update
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                    Logger.Info("Deleted failed update at {Path}", appPath);
                }

                // Restore from backup
                if (Directory.Exists(backupPath))
                {
                    CopyDirectory(backupPath, appPath);
                    Logger.Info("Restored from backup {BackupPath} to {AppPath}", backupPath, appPath);

                    // Restore version info
                    if (!string.IsNullOrEmpty(binaryVersion))
                    {
                        SaveVersionInfo(appCode, binaryVersion, configVersion ?? "0.0.0");
                    }

                    // Keep backup for safety (will be cleaned up later)
                    Logger.Info("Rollback completed successfully");
                }
                else
                {
                    Logger.Error("Backup not found at {BackupPath}, cannot rollback", backupPath);
                    throw new Exception("Backup not found, cannot perform rollback");
                }
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

        /// <summary>
        /// Marks an update as failed to prevent retry loops
        /// </summary>
        private async Task MarkUpdateAsFailedAsync(string appCode, string failedVersion)
        {
            try
            {
                var failureMarkerPath = Path.Combine(_appBasePath, appCode, ".update_failed");
                var failureData = new
                {
                    FailedVersion = failedVersion,
                    Timestamp = DateTime.UtcNow,
                    MachineName = Environment.MachineName
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
                    var failureData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

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