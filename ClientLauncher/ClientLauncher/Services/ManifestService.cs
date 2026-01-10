using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class ManifestService : IManifestService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _manifestBasePath;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ManifestService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };

            // Local manifest storage at C:\CompanyApps
            _manifestBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";

            if (!Directory.Exists(_manifestBasePath))
            {
                Directory.CreateDirectory(_manifestBasePath);
            }

            Logger.Debug("ManifestService initialized with base URL: {BaseUrl}, Manifest path: {Path}",
                _baseUrl, _manifestBasePath);
        }

        public async Task<ManifestDto?> GetManifestFromServerAsync(string appCode)
        {
            try
            {
                // Logger.Info("Fetching manifest from server for {AppCode}", appCode);

                var response = await _httpClient.GetAsync($"/api/apps/{appCode}/manifest");

                if (!response.IsSuccessStatusCode)
                {
                    // Logger.Warn("Failed to get manifest for {AppCode}: {StatusCode}", appCode, response.StatusCode);
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<ManifestDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Logger.Info("Successfully fetched manifest for {AppCode}", appCode);
                    return apiResponse.Data;
                }

                // Logger.Warn("API returned unsuccessful response for {AppCode}", appCode);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error fetching manifest from server for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<ManifestDto?> DownloadManifestFromServerAsync(string appCode)
        {
            try
            {
                // Logger.Info("Downloading manifest file from server for {AppCode}", appCode);

                var response = await _httpClient.GetAsync($"/api/apps/{appCode}/manifest/download");

                if (!response.IsSuccessStatusCode)
                {
                    // Logger.Warn("Failed to download manifest for {AppCode}: {StatusCode}",appCode, response.StatusCode);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<ApiBaseResponse<ManifestDto>>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (manifest != null)
                {
                    // Save to C:\CompanyApps\{appCode}\manifest.json
                    await SaveManifestAsync(appCode, manifest.Data);
                    // Logger.Info("Successfully downloaded and saved manifest for {AppCode}", appCode);
                }

                return manifest.Data;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error downloading manifest for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<ManifestDto?> GetLocalManifestAsync(string appCode)
        {
            try
            {
                var manifestPath = GetLocalManifestPath(appCode);

                if (!File.Exists(manifestPath))
                {
                    Logger.Debug("No local manifest found for {AppCode} at {Path}", appCode, manifestPath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestDto>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Logger.Debug("Successfully loaded local manifest for {AppCode}", appCode);
                return manifest;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading local manifest for {AppCode}", appCode);
                return null;
            }
        }

        public async Task SaveManifestAsync(string appCode, ManifestDto manifest)
        {
            try
            {
                var manifestPath = GetLocalManifestPath(appCode);
                var manifestDir = Path.GetDirectoryName(manifestPath);

                if (!string.IsNullOrEmpty(manifestDir) && !Directory.Exists(manifestDir))
                {
                    Directory.CreateDirectory(manifestDir);
                }

                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(manifestPath, json);
                // Logger.Info("Saved manifest for {AppCode} to {Path}", appCode, manifestPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving manifest for {AppCode}", appCode);
                throw;
            }
        }

        public async Task<bool> IsUpdateAvailableAsync(string appCode, string currentVersion)
        {
            try
            {
                var serverManifest = await GetManifestFromServerAsync(appCode);
                if (serverManifest == null)
                {
                    // Logger.Warn("Cannot check update: server manifest not available for {AppCode}", appCode);
                    return false;
                }

                var serverVersion = serverManifest.Binary?.Version ?? "0.0.0";
                var isNewer = IsNewerVersion(serverVersion, currentVersion);

                //Logger.Info("Update check for {AppCode}: Current={Current}, Server={Server}, UpdateAvailable={Available}", appCode, currentVersion, serverVersion, isNewer);

                return isNewer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking update for {AppCode}", appCode);
                return false;
            }
        }

        public async Task<bool> IsUpdateConfigAvailableAsync(string appCode, string currentVersion)
        {
            try
            {
                var serverManifest = await GetManifestFromServerAsync(appCode);
                if (serverManifest == null)
                {
                    // Logger.Warn("Cannot check update: server manifest not available for {AppCode}", appCode);
                    return false;
                }

                var serverVersion = serverManifest.Config?.Version ?? "0.0.0";
                var isNewer = IsNewerVersion(serverVersion, currentVersion);

                //Logger.Info("Update check for {AppCode}: Current={Current}, Server={Server}, UpdateAvailable={Available}", appCode, currentVersion, serverVersion, isNewer);

                return isNewer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking update for {AppCode}", appCode);
                return false;
            }
        }

        public async Task<string> GetUpdateTypeAsync(string appCode)
        {
            try
            {
                var manifest = await GetManifestFromServerAsync(appCode);
                return manifest?.UpdatePolicy?.Type ?? "none";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting update type for {AppCode}", appCode);
                return "none";
            }
        }

        public async Task<bool> IsUpdateForcedAsync(string appCode)
        {
            try
            {
                var manifest = await GetManifestFromServerAsync(appCode);
                return manifest?.UpdatePolicy?.Force ?? false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking forced update for {AppCode}", appCode);
                return false;
            }
        }

        private string GetLocalManifestPath(string appCode)
        {
            // C:\CompanyApps\{appCode}\manifest.json
            return Path.Combine(_manifestBasePath, appCode, "manifest.json");
        }

        private bool IsNewerVersion(string serverVersion, string localVersion)
        {
            if (string.IsNullOrEmpty(serverVersion))
            {
                Logger.Info("<============ Skip update! ============>");
                return false;
            }

            try
            {
                // Remove 'v' prefix if exists
                serverVersion = serverVersion.TrimStart('v', 'V');
                localVersion = localVersion.TrimStart('v', 'V');

                var server = new Version(serverVersion);
                var local = new Version(localVersion);
                return server > local;
            }
            catch
            {
                // Fallback to string comparison
                return serverVersion != localVersion;
            }
        }
    }
}