using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class VersionCheckService : IVersionCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "https://localhost:7172/api";
        private readonly string _appsBasePath = @"C:\CompanyApps";

        public VersionCheckService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<VersionComparisonResult> CheckForUpdatesAsync(string appCode)
        {
            try
            {
                // 1. Get local version
                var localVersion = GetLocalVersion(appCode);

                // 2. Get server version
                var serverVersion = await GetServerVersionAsync(appCode);

                if (serverVersion == null)
                {
                    return new VersionComparisonResult
                    {
                        UpdateAvailable = false,
                        Message = "Unable to check for updates (server unreachable)"
                    };
                }

                // 3. Compare versions
                var updateAvailable = IsNewerVersion(serverVersion.BinaryVersion, localVersion);

                return new VersionComparisonResult
                {
                    UpdateAvailable = updateAvailable,
                    ForceUpdate = serverVersion.ForceUpdate,
                    LocalVersion = localVersion,
                    ServerVersion = serverVersion.BinaryVersion,
                    UpdateType = serverVersion.UpdateType,
                    Message = updateAvailable
                        ? $"New version available: {serverVersion.BinaryVersion} (Current: {localVersion})"
                        : "You are running the latest version"
                };
            }
            catch (Exception ex)
            {
                return new VersionComparisonResult
                {
                    UpdateAvailable = false,
                    Message = $"Error checking updates: {ex.Message}"
                };
            }
        }

        private string GetLocalVersion(string appCode)
        {
            try
            {
                var versionFile = Path.Combine(_appsBasePath, appCode, "App", "version.txt");

                if (File.Exists(versionFile))
                {
                    return File.ReadAllText(versionFile).Trim();
                }

                return "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }

        private async Task<VersionCheckDto?> GetServerVersionAsync(string appCode)
        {
            try
            {
                var url = $"{_serverUrl}/apps/{appCode}/version";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiBaseResponse<VersionCheckDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return apiResponse?.Success == true ? apiResponse.Data : null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsNewerVersion(string serverVersion, string localVersion)
        {
            try
            {
                var server = new Version(serverVersion);
                var local = new Version(localVersion);
                return server > local;
            }
            catch
            {
                return serverVersion != localVersion;
            }
        }
    }
}
