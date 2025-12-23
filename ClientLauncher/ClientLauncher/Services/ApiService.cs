using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:7172/api";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            Logger.Debug("ApiService initialized with base URL: {BaseUrl}", _baseUrl);
        }

        private async Task<T?> GetApiDataAsync<T>(string endpoint)
        {
            Logger.Debug("Calling API endpoint: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<T>>();

            if (apiResponse?.Success == true)
            {
                Logger.Debug("API call successful for endpoint: {Endpoint}", endpoint);
                return apiResponse.Data;
            }

            Logger.Warn("API call returned unsuccessful response for endpoint: {Endpoint}", endpoint);
            return default;
        }

        public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
        {
            try
            {
                Logger.Info("Fetching all applications from API");
                var apps = await GetApiDataAsync<List<ApplicationDto>>("/api/AppCatalog/applications");
                Logger.Info("Successfully fetched {Count} applications", apps?.Count ?? 0);
                return apps ?? new List<ApplicationDto>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to fetch applications from API");
                throw new Exception($"Failed to fetch applications: {ex.Message}", ex);
            }
        }

        public async Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName)
        {
            try
            {
                Logger.Info("Installing application {AppCode} for user {UserName}", appCode, userName);

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
                    Logger.Info("Successfully installed application {AppCode}", appCode);
                    return apiResponse.Data;
                }

                Logger.Warn("Installation API returned unsuccessful response for {AppCode}", appCode);
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
                Logger.Info("Uninstalling application {AppCode} for user {UserName}", appCode, userName);

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
                    Logger.Info("Successfully uninstalled application {AppCode}", appCode);
                    return apiResponse.Data;
                }

                Logger.Warn("Uninstallation API returned unsuccessful response for {AppCode}", appCode);
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
                Logger.Debug("Checking if application {AppCode} is installed", appCode);
                var result = await GetApiDataAsync<IsInstalledResponse>(
                    $"/api/AppCatalog/applications/{appCode}/installed"
                );
                return result?.IsInstalled ?? false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check installation status for {AppCode}", appCode);
                return false;
            }
        }

        public async Task<VersionInfoDto?> GetServerVersionAsync(string appCode)
        {
            try
            {
                Logger.Debug("Getting server version for application {AppCode}", appCode);
                var response = await _httpClient.GetAsync($"/api/apps/{appCode}/version");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn("Failed to get server version for {AppCode}: {StatusCode}",
                        appCode, response.StatusCode);
                    return null;
                }

                var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfoDto>();
                Logger.Debug("Server version for {AppCode}: {Version}", appCode, versionInfo?.BinaryVersion);
                return versionInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get server version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<string?> GetInstalledVersionAsync(string appCode)
        {
            try
            {
                Logger.Debug("Getting installed version for application {AppCode}", appCode);
                var response = await _httpClient.GetAsync($"/api/AppCatalog/applications/{appCode}/version");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn("Application {AppCode} is not installed", appCode);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<InstalledVersionResponse>();
                Logger.Debug("Installed version for {AppCode}: {Version}", appCode, result?.Version);
                return result?.Version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get installed version for {AppCode}", appCode);
                return null;
            }
        }
    }
}