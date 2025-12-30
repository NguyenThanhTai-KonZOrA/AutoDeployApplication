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
            try
            {
                Logger.Info("Starting update for {AppCode}", appCode);

                // 1. Get latest manifest from server
                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                if (manifest == null)
                {
                    throw new Exception("Failed to retrieve manifest from server");
                }

                var updateType = manifest.UpdatePolicy?.Type ?? "both";
                Logger.Info("Update type: {UpdateType}", updateType);

                var appPath = Path.Combine(_appBasePath, appCode, "App");
                var configPath = Path.Combine(_appBasePath, appCode, "Config");
                var backupPath = Path.Combine(_appBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");

                // 2. Backup current installation
                if (Directory.Exists(appPath))
                {
                    Directory.Move(appPath, backupPath);
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

                    stopwatch.Stop();
                    await NotifyInstallationAsync(appCode, manifest.Binary?.Version ?? string.Empty,
                        true, stopwatch.Elapsed, null, null, "Update");

                    // Update version info
                    SaveVersionInfo(appCode, manifest.Binary?.Version ?? "0.0.0", manifest.Config?.Version ?? "0.0.0");

                    // Update local manifest
                    await _manifestService.SaveManifestAsync(appCode, manifest);

                    // Delete backup on success
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                        Logger.Info("Deleted backup");
                    }

                    Logger.Info("Update completed for {AppCode}", appCode);
                    return new InstallationResult
                    {
                        Success = true,
                        Message = "Update completed successfully",
                        InstalledVersion = manifest.Binary?.Version
                    };
                }
                catch
                {
                    // Rollback on error
                    if (Directory.Exists(appPath))
                    {
                        Directory.Delete(appPath, true);
                    }
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Move(backupPath, appPath);
                        Logger.Info("Rolled back to backup");
                    }

                    stopwatch.Stop();
                    await NotifyInstallationAsync(appCode, manifest.Binary?.Version ?? string.Empty,
                        false, stopwatch.Elapsed, "Rolled back to backup", null, "Update");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update failed for {AppCode}", appCode);
                stopwatch.Stop();
                await NotifyInstallationAsync(appCode, string.Empty, false, stopwatch.Elapsed, ex.Message, null, "Update");
                return new InstallationResult
                {
                    Success = false,
                    Message = "Update failed",
                    ErrorDetails = ex.Message
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
    }
}