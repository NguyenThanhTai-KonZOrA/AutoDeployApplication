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
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _serverUrl = "https://localhost:7172"; // Load from config
        }

        public async Task<AppManifest?> GetManifestAsync(string appCode)
        {
            var appFolder = Path.Combine(_appsBasePath, appCode);
            var manifestPath = Path.Combine(appFolder, "manifest.json");

            // Check if manifest exists locally
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest not found locally at {Path}. Creating folder structure and downloading from server...", manifestPath);

                try
                {
                    // Create folder structure
                    Directory.CreateDirectory(appFolder);
                    _logger.LogInformation("Created app folder: {AppFolder}", appFolder);

                    // ✅ Download manifest file from server
                    bool downloaded = await DownloadManifestFileAsync(appCode, manifestPath);

                    if (downloaded)
                    {
                        _logger.LogInformation("Manifest downloaded successfully for {AppCode}", appCode);

                        // Read and return the downloaded manifest
                        var json = await File.ReadAllTextAsync(manifestPath);
                        var manifest = JsonSerializer.Deserialize<AppManifest>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return manifest;
                    }
                    else
                    {
                        _logger.LogWarning("Could not download manifest from server for {AppCode}. Creating default manifest.", appCode);

                        // Create default manifest
                        var defaultManifest = CreateDefaultManifest(appCode);
                        await UpdateManifestAsync(appCode, defaultManifest);
                        return defaultManifest;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading manifest for {AppCode}", appCode);

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

        /// <summary>
        /// ✅ NEW METHOD: Download manifest.json file from server
        /// Similar to DownloadPackage logic
        /// </summary>
        private async Task<bool> DownloadManifestFileAsync(string appCode, string destinationPath, int retryCount = 0, int maxRetries = 3)
        {
            if (retryCount >= maxRetries)
            {
                _logger.LogError("Max retries ({MaxRetries}) reached for downloading manifest of {AppCode}", maxRetries, appCode);
                return false;
            }

            try
            {
                // ✅ Use download endpoint instead of manifest endpoint
                var downloadUrl = $"{_serverUrl}/api/apps/{appCode}/manifest/download";
                _logger.LogInformation("Downloading manifest from {Url} (Attempt {Attempt}/{MaxRetries})",
                    downloadUrl, retryCount + 1, maxRetries);

                var response = await _httpClient.GetAsync(downloadUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download manifest: {StatusCode}. Retry {Retry}/{MaxRetries}",
                        response.StatusCode, retryCount + 1, maxRetries);

                    // Only retry on server errors (5xx)
                    if ((int)response.StatusCode >= 500 && retryCount + 1 < maxRetries)
                    {
                        await Task.Delay(1000 * (retryCount + 1)); // Exponential backoff
                        return await DownloadManifestFileAsync(appCode, destinationPath, retryCount + 1, maxRetries);
                    }

                    // Don't retry on 404, 400, etc.
                    _logger.LogError("Manifest not found or client error for {AppCode}: {StatusCode}",
                        appCode, response.StatusCode);
                    return false;
                }

                // ✅ Read file content as bytes (similar to DownloadPackage)
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Downloaded {Size} bytes for manifest.json", fileBytes.Length);

                // ✅ Validate that it's valid JSON before saving
                try
                {
                    var jsonString = System.Text.Encoding.UTF8.GetString(fileBytes);
                    var testManifest = JsonSerializer.Deserialize<AppManifest>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (testManifest == null || string.IsNullOrEmpty(testManifest.appCode))
                    {
                        _logger.LogError("Downloaded manifest is invalid (null or empty appCode)");
                        return false;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Downloaded content is not valid JSON");
                    return false;
                }

                // ✅ Save file to disk
                await File.WriteAllBytesAsync(destinationPath, fileBytes);
                _logger.LogInformation("Manifest file saved to {Path}", destinationPath);

                return true;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error downloading manifest for {AppCode}", appCode);

                // Retry on network errors
                if (retryCount + 1 < maxRetries)
                {
                    await Task.Delay(1000 * (retryCount + 1));
                    return await DownloadManifestFileAsync(appCode, destinationPath, retryCount + 1, maxRetries);
                }

                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout downloading manifest for {AppCode}", appCode);

                // Retry on timeout
                if (retryCount + 1 < maxRetries)
                {
                    await Task.Delay(1000 * (retryCount + 1));
                    return await DownloadManifestFileAsync(appCode, destinationPath, retryCount + 1, maxRetries);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading manifest for {AppCode}", appCode);
                return false;
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
                    package = $"{appCode}_v0.0.1.zip",
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