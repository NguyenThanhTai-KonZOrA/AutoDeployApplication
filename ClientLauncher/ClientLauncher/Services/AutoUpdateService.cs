using ClientLauncher.Models.Response;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ClientLauncher.Services
{
    public class AutoUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _currentVersion;
        private readonly string _versionFilePath;
        private const string APP_CODE = "ClientApplication";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public AutoUpdateService()
        {
            _baseUrl = System.Configuration.ConfigurationManager.AppSettings["ClientLauncherBaseUrl"]
                ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _currentVersion = GetCurrentVersion();

            // Đường dẫn file lưu version đã cài đặt
            var installDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "";
            _versionFilePath = Path.Combine(installDir, "version.txt");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                // Đọc version đã cài đặt từ file local
                var installedVersion = GetInstalledVersion();
                Logger.Info("Checking for ClientLauncher updates... Installed version: {0}", installedVersion);

                // Call API mới để lấy thông tin application
                var response = await _httpClient.GetAsync($"/api/ApplicationManagement/code/{APP_CODE}");
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn("Update check failed: {0}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var appDetail = JsonSerializer.Deserialize<ApiBaseResponse<ApplicationDetailResponse>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (appDetail != null && !string.IsNullOrEmpty(appDetail.Data.LatestVersion))
                {
                    Logger.Info("Latest version available: {0}", appDetail.Data.LatestVersion);

                    if (IsNewerVersion(appDetail.Data.LatestVersion, installedVersion))
                    {
                        Logger.Info("✅ New version available: {0} -> {1}", installedVersion, appDetail.Data.LatestVersion);

                        return new UpdateInfo
                        {
                            Version = appDetail.Data.LatestVersion,
                            PackageId = appDetail.Data.PackageId ?? 0,
                            DownloadUrl = appDetail.Data.PackageUrl ?? string.Empty,
                            ReleaseNotes = appDetail.Data.ReleaseNotes ?? "No release notes available",
                            ReleasedAt = appDetail.Data.LatestVersionDate ?? DateTime.Now,
                            FileSizeBytes = 0,
                            IsCritical = false
                        };
                    }
                    else
                    {
                        Logger.Info("✅ Already on latest version: {0}", installedVersion);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking for updates");
                return null;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
        {
            try
            {
                Logger.Info("Downloading update {0}...", updateInfo.Version);

                var updateFilePath = Path.Combine(Path.GetTempPath(), "ClientLauncher_Update.zip");

                using (var response = await _httpClient.GetAsync($"api/PackageManagement/{updateInfo.PackageId}/download", HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(updateFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (canReportProgress)
                            {
                                progress?.Report((int)((totalRead * 100) / totalBytes));
                            }
                        }
                    }
                }

                Logger.Info("Download completed: {0}", updateFilePath);

                // Lưu version mới vào file trước khi update
                SaveInstalledVersion(updateInfo.Version);
                Logger.Info("Saved new version to file: {0}", updateInfo.Version);

                // Create updater script
                var updaterScript = CreateUpdaterScript(updateFilePath, updateInfo.Version);
                Logger.Info("Starting updater: {0}", updaterScript);

                // Launch updater and exit current app
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{updaterScript}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                Logger.Info("Update process initiated. Exiting application...");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error downloading/installing update");
                return false;
            }
        }

        private string CreateUpdaterScript(string updateFilePath, string newVersion)
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "ClientLauncher_Updater.bat");
            var currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            var installDir = Path.GetDirectoryName(currentExePath) ?? "";
            var backupDir = Path.Combine(installDir, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}");

            var script = $@"@echo off
echo ============================================
echo  ClientLauncher Auto-Update
echo  Version: {newVersion}
echo ============================================

REM Wait for application to close
timeout /t 2 /nobreak >nul

echo Creating backup...
mkdir ""{backupDir}""
xcopy /E /I /Y ""{installDir}\*.*"" ""{backupDir}"" >nul 2>&1

echo Extracting update...
powershell -Command ""Expand-Archive -Path '{updateFilePath}' -DestinationPath '{installDir}' -Force""

if %errorlevel% neq 0 (
    echo ERROR: Update extraction failed
    echo Restoring from backup...
    xcopy /E /I /Y ""{backupDir}\*.*"" ""{installDir}"" >nul 2>&1
    pause
    exit /b 1
)

echo Cleaning up...
del ""{updateFilePath}""

echo Starting ClientLauncher...
start """" ""{currentExePath}""

echo ============================================
echo  Update completed successfully!
echo ============================================

REM Self-delete
del ""%~f0""
";

            File.WriteAllText(scriptPath, script);
            return scriptPath;
        }

        private string GetCurrentVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private bool IsNewerVersion(string remoteVersion, string currentVersion)
        {
            try
            {
                var remote = new Version(remoteVersion);
                var current = new Version(currentVersion);
                return remote > current;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Đọc version đã cài đặt từ file local
        /// </summary>
        private string GetInstalledVersion()
        {
            try
            {
                if (File.Exists(_versionFilePath))
                {
                    var version = File.ReadAllText(_versionFilePath).Trim();
                    if (!string.IsNullOrEmpty(version))
                    {
                        Logger.Debug("Installed version from file: {0}", version);
                        return version;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to read installed version from file, using assembly version");
            }

            // Fallback: sử dụng version từ assembly và lưu vào file
            var assemblyVersion = _currentVersion;
            SaveInstalledVersion(assemblyVersion);
            return assemblyVersion;
        }

        /// <summary>
        /// Lưu version đã cài đặt vào file local
        /// </summary>
        private void SaveInstalledVersion(string version)
        {
            try
            {
                File.WriteAllText(_versionFilePath, version);
                Logger.Debug("Saved installed version to file: {0}", version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save installed version to file");
            }
        }

        public async Task<bool> AutoCheckAndUpdateAsync(bool silent = true)
        {
            var updateInfo = await CheckForUpdatesAsync();
            if (updateInfo == null)
                return false;

            var installedVersion = GetInstalledVersion();

            if (!silent)
            {
                var result = MessageBox.Show(
                    $"A new version of ClientLauncher is available!\n\n" +
                    $"Installed: {installedVersion}\n" +
                    $"Latest: {updateInfo.Version}\n\n" +
                    $"Release Notes:\n{updateInfo.ReleaseNotes}\n\n" +
                    $"Would you like to update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                    return false;
            }

            return await DownloadAndInstallUpdateAsync(updateInfo);
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsCritical { get; set; }
    }

    public class ApplicationDetailResponse
    {
        public int Id { get; set; }
        public int ManifestId { get; set; }
        public string ManifestVersion { get; set; } = string.Empty;
        public string ManifestBinaryVersion { get; set; } = string.Empty;
        public string ManifestConfigVersion { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
        public string? Developer { get; set; }
        public string? SupportEmail { get; set; }
        public string? DocumentationUrl { get; set; }
        public bool RequiresAdminRights { get; set; }
        public string? MinimumOsVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LatestVersion { get; set; }
        public DateTime? LatestVersionDate { get; set; }
        public int TotalVersions { get; set; }
        public int TotalInstalls { get; set; }
        public long TotalStorageSize { get; set; }
        public int? PackageId { get; set; }
        public string? PackageFileName { get; set; }
        public string? PackageType { get; set; }
        public string? PackageVersion { get; set; }
        public string? PackageUrl { get; set; }
        public bool? IsStable { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? MinimumClientVersion { get; set; }
    }
}
