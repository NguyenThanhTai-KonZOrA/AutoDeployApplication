using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IInstallationChecker _installationChecker;
        private readonly IManifestService _manifestService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ApiService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _installationChecker = new InstallationChecker();
            _manifestService = new ManifestService();

            //  Logger.Debug("ApiService initialized with base URL: {BaseUrl}", _baseUrl);
        }

        private async Task<T?> GetApiDataAsync<T>(string endpoint)
        {
            //Logger.Debug("Calling API endpoint: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<T>>();

            if (apiResponse?.Success == true)
            {
                //Logger.Debug("API call successful for endpoint: {Endpoint}", endpoint);
                return apiResponse.Data;
            }

            // Logger.Warn("API call returned unsuccessful response for endpoint: {Endpoint}", endpoint);
            return default;
        }

        public async Task<bool> IsAdminRole()
        {
            try
            {
                // Logger.Info("Fetching all applications from API");
                var isAdmin = await GetApiDataAsync<bool>($"/api/Auth/roles/check/{Environment.UserName}");
                // Logger.Info("Successfully fetched {Count} applications", apps?.Count ?? 0);
                return isAdmin;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to fetch applications from API");
                throw new Exception($"Failed to fetch applications: {ex.Message}", ex);
            }
        }
        public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
        {
            try
            {
                // Logger.Info("Fetching all applications from API");
                var apps = await GetApiDataAsync<List<ApplicationDto>>($"/api/AppCatalog/applications?userName={Environment.UserName}");
                // Logger.Info("Successfully fetched {Count} applications", apps?.Count ?? 0);
                return apps ?? new List<ApplicationDto>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to fetch applications from API");
                throw new Exception($"Failed to fetch applications: {ex.Message}", ex);
            }
        }

        public async Task<ManifestApplicationResponse?> GetApplicationByCodeAsync(string appCode)
        {
            try
            {
                // Logger.Info("Fetching application {AppCode} from API", appCode);
                var app = await GetApiDataAsync<ManifestApplicationResponse>($"/api/ApplicationManagement/manifest/latest/{appCode}");
                if (app != null)
                {
                    // Logger.Info("Successfully fetched manifest for application {AppCode}", appCode);
                }
                else
                {
                    // Logger.Warn("Application {AppCode} not found in API", appCode);
                }
                return app;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to fetch application {AppCode} from API", appCode);
                throw new Exception($"Failed to fetch application {appCode}: {ex.Message}", ex);
            }
        }

        public async Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName)
        {
            try
            {
                // Logger.Info("Installing application {AppCode} for user {UserName}", appCode, userName);

                var request = new InstallationRequestDto
                {
                    AppCode = appCode,
                    UserName = userName
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Installation/install", content);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<InstallationResultDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Logger.Info("Successfully installed application {AppCode}", appCode);
                    return apiResponse.Data;
                }

                // Logger.Warn("Installation API returned unsuccessful response for {AppCode}", appCode);
                return new InstallationResultDto { Success = false, Message = "Unknown error" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to install application {AppCode}", appCode);
                return new InstallationResultDto
                {
                    Success = false,
                    Message = "Installation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<InstallationResultDto> UninstallApplicationAsync(string appCode, string userName)
        {
            try
            {
                // Logger.Info("Uninstalling application {AppCode} for user {UserName}", appCode, userName);

                var request = new InstallationRequestDto
                {
                    AppCode = appCode,
                    UserName = userName
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Installation/uninstall", content);
                response.EnsureSuccessStatusCode();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<InstallationResultDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Logger.Info("Successfully uninstalled application {AppCode}", appCode);
                    return apiResponse.Data;
                }

                // Logger.Warn("Uninstallation API returned unsuccessful response for {AppCode}", appCode);
                return new InstallationResultDto { Success = false, Message = "Unknown error" };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to uninstall application {AppCode}", appCode);
                return new InstallationResultDto
                {
                    Success = false,
                    Message = "Uninstallation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<bool> IsApplicationInstalledAsync(string appCode)
        {
            try
            {
                //  Logger.Debug("Checking if application {AppCode} is installed locally", appCode);
                var isInstalled = _installationChecker.IsApplicationInstalled(appCode);
                // Logger.Info("Application {AppCode} installation status: {IsInstalled}", appCode, isInstalled);
                return isInstalled;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check installation status for {AppCode}", appCode);
                return false;
            }
        }

        public async Task<string?> GetInstalledVersionAsync(string appCode)
        {
            try
            {
                //  Logger.Debug("Getting installed version for application {AppCode}", appCode);
                var version = _installationChecker.GetInstalledVersion(appCode);

                if (version != null)
                {
                    //  Logger.Debug("Installed version for {AppCode}: {Version}", appCode, version);
                }
                else
                {
                    //  Logger.Debug("No installed version found for {AppCode}", appCode);
                }

                return version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get installed version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<string?> GetInstalledBinaryVersionAsync(string appCode)
        {
            try
            {
                //  Logger.Debug("Getting installed version for application {AppCode}", appCode);
                var version = _installationChecker.GetInstalledBinaryVersion(appCode);

                if (version != null)
                {
                    //  Logger.Debug("Installed version for {AppCode}: {Version}", appCode, version);
                }
                else
                {
                    //  Logger.Debug("No installed version found for {AppCode}", appCode);
                }

                return version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get installed version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<string?> GetInstalledConfigVersionAsync(string appCode)
        {
            try
            {
                //  Logger.Debug("Getting installed version for application {AppCode}", appCode);
                var version = _installationChecker.GetInstalledConfigVersion(appCode);

                if (version != null)
                {
                    //  Logger.Debug("Installed version for {AppCode}: {Version}", appCode, version);
                }
                else
                {
                    //  Logger.Debug("No installed version found for {AppCode}", appCode);
                }

                return version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get installed version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<VersionInfoDto?> GetServerVersionAsync(string appCode)
        {
            try
            {
                //  Logger.Debug("Getting server version for application {AppCode}", appCode);

                // Use manifest service to get version from database-generated manifest
                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);

                if (manifest == null)
                {
                    // Logger.Warn("No manifest found for {AppCode}", appCode);
                    return null;
                }

                var versionInfo = new VersionInfoDto
                {
                    BinaryVersion = manifest.Binary?.Version ?? "0.0.0",
                    ConfigVersion = manifest.Config?.Version ?? manifest.Binary?.Version ?? "0.0.0",
                    UpdateType = manifest.UpdatePolicy?.Type ?? "none",
                    ForceUpdate = manifest.UpdatePolicy?.Force ?? false
                };

                //Logger.Debug("Server version for {AppCode}: Binary={BinaryVersion}, Config={ConfigVersion}", appCode, versionInfo.BinaryVersion, versionInfo.ConfigVersion);

                return versionInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get server version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task NotifyInstallationAsync(string appCode, string version, bool success, TimeSpan duration, string? error = null, string? oldVersion = null, string action = "Install")
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
                    // Logger.Info("Installation logged to server successfully");
                }
                else
                {
                    // Logger.Warn($"Failed to log installation: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Logger.Warn(ex, "Failed to notify server about installation (non-critical)");
            }
        }
    }
}