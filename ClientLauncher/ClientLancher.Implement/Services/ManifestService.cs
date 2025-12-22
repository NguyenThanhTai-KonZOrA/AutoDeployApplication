using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClientLancher.Implement.Services
{
    public class ManifestService : IManifestService
    {
        private readonly string _appsBasePath;
        private readonly ILogger<ManifestService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;

        public ManifestService(ILogger<ManifestService> logger, HttpClient httpClient)
        {
            _appsBasePath = "C:\\CompanyApps";
            _logger = logger;
            _httpClient = httpClient;
            _serverUrl = "https://localhost:7172"; // Load from config
        }

        public async Task<AppManifest?> GetManifestAsync(string appCode)
        {
            var appFolder = Path.Combine(_appsBasePath, appCode);
            var manifestPath = Path.Combine(appFolder, "manifest.json");

            // Check if manifest exists locally
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest not found locally at {Path}. Creating folder structure and fetching from server...", manifestPath);

                // Create folder structure
                try
                {
                    Directory.CreateDirectory(appFolder);
                    _logger.LogInformation("Created app folder: {AppFolder}", appFolder);

                    // Try to fetch manifest from server
                    var manifest = await FetchManifestFromServerAsync(appCode);

                    if (manifest != null)
                    {
                        // Save manifest locally
                        await UpdateManifestAsync(appCode, manifest);
                        _logger.LogInformation("Manifest fetched from server and saved locally for {AppCode}", appCode);
                        return manifest;
                    }
                    else
                    {
                        _logger.LogWarning("Could not fetch manifest from server for {AppCode}. Creating default manifest.", appCode);

                        // Create default manifest
                        var defaultManifest = CreateDefaultManifest(appCode);
                        await UpdateManifestAsync(appCode, defaultManifest);
                        return defaultManifest;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating folder structure or fetching manifest for {AppCode}", appCode);

                    // FALLBACK: Create default manifest on error
                    var fallbackManifest = CreateDefaultManifest(appCode);
                    await UpdateManifestAsync(appCode, fallbackManifest);
                    return fallbackManifest;
                }
            }

            // Read existing manifest
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<AppManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _logger.LogInformation("Manifest loaded from local path for {AppCode}", appCode);
                return manifest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading manifest file for {AppCode}", appCode);
                return null;
            }
        }

        public async Task UpdateManifestAsync(string appCode, AppManifest manifest)
        {
            var appFolder = Path.Combine(_appsBasePath, appCode);
            Directory.CreateDirectory(appFolder);

            var manifestPath = Path.Combine(appFolder, "manifest.json");
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(manifestPath, json);
            _logger.LogInformation("Manifest updated for {AppCode} at {Path}", appCode, manifestPath);
        }

        private async Task<AppManifest?> FetchManifestFromServerAsync(string appCode, int retryCount = 0, int maxRetries = 3)
        {
            // ✅ FIX: Kiểm tra retry count ngay từ đầu
            if (retryCount >= maxRetries)
            {
                _logger.LogError("Max retries ({MaxRetries}) reached for fetching manifest of {AppCode}", maxRetries, appCode);
                return null;
            }

            try
            {
                var manifestUrl = $"{_serverUrl}/api/apps/{appCode}/manifest";
                _logger.LogInformation("Fetching manifest from {Url} (Attempt {Attempt}/{MaxRetries})", manifestUrl, retryCount + 1, maxRetries);

                var response = await _httpClient.GetAsync(manifestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch manifest from server: {StatusCode}. Retry {Retry}/{MaxRetries}",
                        response.StatusCode, retryCount + 1, maxRetries);

                    // ✅ FIX: Chỉ retry nếu chưa đạt maxRetries
                    if (retryCount + 1 < maxRetries)
                    {
                        await Task.Delay(1000 * (retryCount + 1)); // Exponential backoff
                        return await FetchManifestFromServerAsync(appCode, retryCount + 1, maxRetries);
                    }

                    // ✅ Đã hết retry, return null
                    _logger.LogError("All retry attempts failed for {AppCode}", appCode);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Server response: {Response}", responseContent);

                // Try to deserialize as ApiBaseResponse first
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiBaseResponse<AppManifest>>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }

                    return null;
                }
                catch (JsonException)
                {
                    // If ApiBaseResponse deserialization fails, try direct deserialization
                    _logger.LogDebug("Failed to deserialize as ApiBaseResponse, trying direct deserialization");

                    var manifest = JsonSerializer.Deserialize<AppManifest>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (manifest != null)
                    {
                        _logger.LogInformation("Manifest successfully fetched (direct) from server for {AppCode}", appCode);
                        return manifest;
                    }
                }

                _logger.LogWarning("Could not deserialize manifest response from server");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error fetching manifest from server for {AppCode}", appCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching manifest from server for {AppCode}", appCode);
                return null;
            }
        }

        private AppManifest CreateDefaultManifest(string appCode)
        {
            _logger.LogInformation("Creating default manifest for {AppCode}", appCode);

            return new AppManifest
            {
                appCode = appCode,
                binary = new BinaryInfo
                {
                    version = "0.0.1",
                    package = $"{appCode}_v0.0.1.zip"
                },
                config = new ConfigInfo
                {
                    version = "0.0.1",
                    package = "config.json",
                    mergeStrategy = "preserveLocal"
                },
                updatePolicy = new UpdatePolicy
                {
                    type = "both",
                    force = false
                }
            };
        }
    }
}