using ClientLauncher.Models;
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

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
        }

        // 🔥 Generic helper method để xử lý API response
        private async Task<T?> GetApiDataAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<T>>();

            if (apiResponse?.Success == true)
            {
                return apiResponse.Data;
            }

            return default;
        }

        public async Task<List<ApplicationDto>> GetAllApplicationsAsync()
        {
            try
            {
                var apps = await GetApiDataAsync<List<ApplicationDto>>("/api/AppCatalog/applications");
                return apps ?? new List<ApplicationDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch applications: {ex.Message}", ex);
            }
        }

        public async Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName)
        {
            try
            {
                var request = new InstallationRequestDto
                {
                    AppCode = appCode,
                    UserName = userName
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Installation/install", content);
                response.EnsureSuccessStatusCode();

                // Nếu Installation API cũng trả về ApiBaseResponse
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiBaseResponse<InstallationResultDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }

                return new InstallationResultDto { Success = false, Message = "Unknown error" };
            }
            catch (Exception ex)
            {
                return new InstallationResultDto
                {
                    Success = false,
                    Message = "Installation failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// ✅ NEW: Uninstall application
        /// </summary>
        public async Task<InstallationResultDto> UninstallApplicationAsync(string appCode, string userName)
        {
            try
            {
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
                    return apiResponse.Data;
                }

                return new InstallationResultDto { Success = false, Message = "Unknown error" };
            }
            catch (Exception ex)
            {
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
                var result = await GetApiDataAsync<IsInstalledResponse>(
                    $"/api/AppCatalog/applications/{appCode}/installed"
                );

                return result?.IsInstalled ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ✅ NEW: Get server version from manifest
        /// </summary>
        public async Task<VersionInfoDto?> GetServerVersionAsync(string appCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/apps/{appCode}/version");

                if (!response.IsSuccessStatusCode)
                    return null;

                var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfoDto>();
                return versionInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ✅ NEW: Get installed version
        /// </summary>
        public async Task<string?> GetInstalledVersionAsync(string appCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/AppCatalog/applications/{appCode}/version");

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<InstalledVersionResponse>();
                return result?.Version;
            }
            catch
            {
                return null;
            }
        }
    }

    public class IsInstalledResponse
    {
        public string AppCode { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
    }

    /// <summary>
    /// ✅ NEW: Version info DTO
    /// </summary>
    public class VersionInfoDto
    {
        public string AppCode { get; set; } = string.Empty;
        public string BinaryVersion { get; set; } = string.Empty;
        public string ConfigVersion { get; set; } = string.Empty;
        public string UpdateType { get; set; } = string.Empty;
        public bool ForceUpdate { get; set; }
    }

    public class InstalledVersionResponse
    {
        public string AppCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}