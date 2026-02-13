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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public AutoUpdateService()
        {
            _baseUrl = System.Configuration.ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] 
                ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _currentVersion = GetCurrentVersion();
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                Logger.Info("Checking for ClientLauncher updates... Current version: {0}", _currentVersion);

                var response = await _httpClient.GetAsync("/api/update/clientlauncher/check");
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn("Update check failed: {0}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<UpdateInfo>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Success == true && result.Data != null)
                {
                    var updateInfo = result.Data;
                    Logger.Info("Latest version available: {0}", updateInfo.Version);

                    if (IsNewerVersion(updateInfo.Version, _currentVersion))
                    {
                        Logger.Info("✅ New version available: {0} -> {1}", _currentVersion, updateInfo.Version);
                        return updateInfo;
                    }
                    else
                    {
                        Logger.Info("✅ Already on latest version: {0}", _currentVersion);
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

                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
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

        public async Task<bool> AutoCheckAndUpdateAsync(bool silent = true)
        {
            var updateInfo = await CheckForUpdatesAsync();
            if (updateInfo == null)
                return false;

            if (!silent)
            {
                var result = MessageBox.Show(
                    $"A new version of ClientLauncher is available!\n\n" +
                    $"Current: {_currentVersion}\n" +
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
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsCritical { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
