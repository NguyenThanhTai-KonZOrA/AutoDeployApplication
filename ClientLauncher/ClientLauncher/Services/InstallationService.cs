using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
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
        private readonly string _serverUrl;
        private readonly string _appsBasePath;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public InstallationService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

            // Load from config
            _serverUrl = System.Configuration.ConfigurationManager.AppSettings["ServerUrl"] ?? "http://10.21.10.1:8102";
            _appsBasePath = System.Configuration.ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";

            _logger.Info($"InstallationService initialized. Server: {_serverUrl}, AppsPath: {_appsBasePath}");
        }

        public async Task<InstallationResult> InstallApplicationAsync(string appCode, string packageName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new InstallationResult { AppCode = appCode };

            try
            {
                _logger.Info($"Starting installation for {appCode}, package: {packageName}");

                // 1. Download package
                _logger.Info($"Downloading package {packageName} from server...");
                var packageBytes = await DownloadPackageAsync(appCode, packageName);

                if (packageBytes == null || packageBytes.Length == 0)
                {
                    throw new Exception("Failed to download package or package is empty");
                }

                _logger.Info($"Downloaded {packageBytes.Length} bytes");

                // 2. Save to temp
                var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_install_{Guid.NewGuid()}.zip");
                await File.WriteAllBytesAsync(tempZip, packageBytes);

                // 3. Extract to app folder
                var appPath = Path.Combine(_appsBasePath, appCode, "App");

                // ✅ If exists, this is an UPDATE - backup first
                string? backupPath = null;
                if (Directory.Exists(appPath))
                {
                    backupPath = Path.Combine(_appsBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");
                    _logger.Info($"Existing installation found, creating backup at {backupPath}");

                    Directory.CreateDirectory(backupPath);
                    CopyDirectory(appPath, backupPath);

                    // Delete old files
                    Directory.Delete(appPath, true);
                }

                try
                {
                    Directory.CreateDirectory(appPath);
                    _logger.Info($"Extracting to {appPath}...");
                    ZipFile.ExtractToDirectory(tempZip, appPath, overwriteFiles: true);

                    // Get version
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(packageName, @"_v?(\d+\.\d+\.\d+)");
                    var installedVersion = versionMatch.Success ? versionMatch.Groups[1].Value : "1.0.0";

                    // Save version.txt
                    var versionFile = Path.Combine(appPath, "version.txt");
                    await File.WriteAllTextAsync(versionFile, installedVersion);

                    // Delete backup on success
                    if (backupPath != null && Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                        _logger.Info("Backup deleted after successful installation");
                    }

                    // Cleanup temp
                    if (File.Exists(tempZip))
                        File.Delete(tempZip);

                    stopwatch.Stop();

                    result.Success = true;
                    result.InstalledVersion = installedVersion;
                    result.Message = $"Installation completed successfully in {stopwatch.Elapsed.TotalSeconds:F2}s";
                    result.InstallationPath = appPath;

                    await NotifyInstallationAsync(appCode, installedVersion, true, stopwatch.Elapsed);

                    return result;
                }
                catch
                {
                    // Rollback on error
                    if (backupPath != null && Directory.Exists(backupPath))
                    {
                        if (Directory.Exists(appPath))
                            Directory.Delete(appPath, true);

                        Directory.Move(backupPath, appPath);
                        _logger.Info("Rollback completed");
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"Installation failed for {appCode}");

                result.Success = false;
                result.Message = "Installation failed";
                result.ErrorDetails = ex.Message;

                await NotifyInstallationAsync(appCode, "0.0.0", false, stopwatch.Elapsed, ex.Message);

                return result;
            }
        }

        public async Task<InstallationResult> UpdateApplicationAsync(string appCode, string packageName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new InstallationResult { AppCode = appCode };

            try
            {
                _logger.Info($"Starting update for {appCode}, package: {packageName}");

                var appPath = Path.Combine(_appsBasePath, appCode, "App");

                // 1. Check if installed
                if (!Directory.Exists(appPath))
                {
                    _logger.Warn($"{appCode} is not installed. Redirecting to install...");
                    return await InstallApplicationAsync(appCode, packageName);
                }

                // 2. Get current version
                var versionFile = Path.Combine(appPath, "version.txt");
                var oldVersion = File.Exists(versionFile) ? (await File.ReadAllTextAsync(versionFile)).Trim() : "0.0.0";
                _logger.Info($"Current version: {oldVersion}");

                // 3. Backup current version
                var backupPath = Path.Combine(_appsBasePath, appCode, $"Backup_{DateTime.Now:yyyyMMddHHmmss}");
                _logger.Info($"Creating backup at {backupPath}...");

                Directory.CreateDirectory(backupPath);
                CopyDirectory(appPath, backupPath);

                try
                {
                    // 4. Download new package
                    _logger.Info($"Downloading update package {packageName}...");
                    var packageBytes = await DownloadPackageAsync(appCode, packageName);

                    if (packageBytes == null || packageBytes.Length == 0)
                    {
                        throw new Exception("Failed to download update package");
                    }

                    // 5. Save to temp
                    var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_update_{Guid.NewGuid()}.zip");
                    await File.WriteAllBytesAsync(tempZip, packageBytes);

                    // 6. Delete old files (keep backup)
                    Directory.Delete(appPath, true);
                    Directory.CreateDirectory(appPath);

                    // 7. Extract new version
                    _logger.Info($"Extracting update to {appPath}...");
                    ZipFile.ExtractToDirectory(tempZip, appPath, overwriteFiles: true);

                    // 8. Get new version
                    var newVersionFile = Path.Combine(appPath, "version.txt");
                    var newVersion = "1.0.0";

                    if (File.Exists(newVersionFile))
                    {
                        newVersion = (await File.ReadAllTextAsync(newVersionFile)).Trim();
                    }
                    else
                    {
                        var versionMatch = System.Text.RegularExpressions.Regex.Match(packageName, @"_v?(\d+\.\d+\.\d+)");
                        if (versionMatch.Success)
                        {
                            newVersion = versionMatch.Groups[1].Value;
                            await File.WriteAllTextAsync(newVersionFile, newVersion);
                        }
                    }

                    // 9. Cleanup
                    if (File.Exists(tempZip))
                        File.Delete(tempZip);

                    // 10. Delete backup on success
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                        _logger.Info("Backup deleted after successful update");
                    }

                    stopwatch.Stop();

                    result.Success = true;
                    result.InstalledVersion = newVersion;
                    result.Message = $"Update completed successfully from {oldVersion} to {newVersion} in {stopwatch.Elapsed.TotalSeconds:F2}s";
                    result.InstallationPath = appPath;

                    _logger.Info($"Update completed: {appCode} {oldVersion} → {newVersion}");

                    await NotifyInstallationAsync(appCode, newVersion, true, stopwatch.Elapsed, oldVersion: oldVersion);

                    return result;
                }
                catch (Exception)
                {
                    // Rollback on error
                    _logger.Error("Update failed, rolling back...");

                    if (Directory.Exists(appPath))
                        Directory.Delete(appPath, true);

                    if (Directory.Exists(backupPath))
                    {
                        Directory.Move(backupPath, appPath);
                        _logger.Info("Rollback completed, restored from backup");
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, $"Update failed for {appCode}");

                result.Success = false;
                result.Message = "Update failed";
                result.ErrorDetails = ex.Message;

                await NotifyInstallationAsync(appCode, "0.0.0", false, stopwatch.Elapsed, ex.Message);

                return result;
            }
        }

        public async Task<bool> UninstallApplicationAsync(string appCode)
        {
            try
            {
                _logger.Info($"Uninstalling {appCode}...");

                var appFolder = Path.Combine(_appsBasePath, appCode);

                if (!Directory.Exists(appFolder))
                {
                    _logger.Warn($"{appCode} is not installed");
                    return false;
                }

                Directory.Delete(appFolder, true);
                _logger.Info($"Uninstalled {appCode} successfully");

                await NotifyInstallationAsync(appCode, "0.0.0", true, TimeSpan.Zero, action: "Uninstall");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Uninstall failed for {appCode}");
                return false;
            }
        }

        private async Task<byte[]?> DownloadPackageAsync(string appCode, string packageName)
        {
            try
            {
                var downloadUrl = $"{_serverUrl}/api/apps/{appCode}/download/{packageName}";
                _logger.Info($"Downloading from: {downloadUrl}");

                var response = await _httpClient.GetAsync(downloadUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"Download failed: {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error downloading package {packageName}");
                return null;
            }
        }

        private async Task NotifyInstallationAsync(string appCode, string version, bool success, TimeSpan duration, string? error = null, string? oldVersion = null, string action = "Install")
        {
            try
            {
                var logUrl = $"{_serverUrl}/api/Installation/log";
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
                    _logger.Info("Installation logged to server successfully");
                }
                else
                {
                    _logger.Warn($"Failed to log installation: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to notify server about installation (non-critical)");
            }
        }

        // Helper method
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
