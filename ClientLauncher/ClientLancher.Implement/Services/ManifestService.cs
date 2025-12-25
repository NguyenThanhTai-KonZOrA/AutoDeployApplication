using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels;
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
        private readonly DeploymentSettings _deploymentSettings;

        public ManifestService(ILogger<ManifestService> logger, HttpClient httpClient, DeploymentSettings deploymentSettings)
        {
            _appsBasePath = "C:\\CompanyApps";
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _deploymentSettings = deploymentSettings;
        }

        public async Task<AppManifest?> GetManifestAsync(string appCode)
        {
            var appFolder = Path.Combine(_appsBasePath, appCode);
            var manifestPath = Path.Combine(appFolder, "manifest.json");

            // STEP 1: Check if manifest exists locally
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest not found locally at {Path}. Creating folder structure and downloading from server...", manifestPath);

                try
                {
                    Directory.CreateDirectory(appFolder);
                    _logger.LogInformation("Created app folder: {AppFolder}", appFolder);

                    // Download manifest file from server
                    bool downloaded = await DownloadManifestFileAsync(appCode, manifestPath);

                    if (downloaded)
                    {
                        _logger.LogInformation("Manifest downloaded successfully for {AppCode}", appCode);

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

                        var defaultManifest = CreateDefaultManifest(appCode);
                        await UpdateManifestAsync(appCode, defaultManifest);
                        return defaultManifest;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading manifest for {AppCode}", appCode);

                    var fallbackManifest = CreateDefaultManifest(appCode);
                    await UpdateManifestAsync(appCode, fallbackManifest);
                    return fallbackManifest;
                }
            }

            // STEP 2: Local manifest exists - Download from server to check version
            try
            {
                _logger.LogInformation("Local manifest found for {AppCode}. Downloading latest manifest from server to check version...", appCode);

                // Read local manifest first
                var localJson = await File.ReadAllTextAsync(manifestPath);
                var localManifest = JsonSerializer.Deserialize<ManifestVersionReponse>(localJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (localManifest == null)
                {
                    _logger.LogError("Failed to deserialize local manifest for {AppCode}", appCode);
                    return null;
                }

                var localVersion = localManifest.data?.binary?.version ?? "0.0.0";
                _logger.LogInformation("Current local version for {AppCode}: {Version}", appCode, localVersion);

                // Create temp path for server manifest
                var tempManifestPath = Path.Combine(appFolder, "manifest.tmp.json");

                // Download manifest file from server to temp location
                bool downloaded = await DownloadManifestFileAsync(appCode, tempManifestPath);

                if (!downloaded)
                {
                    _logger.LogWarning("Could not download server manifest for {AppCode}. Using local manifest.", appCode);

                    // Clean up temp file if exists
                    if (File.Exists(tempManifestPath))
                    {
                        File.Delete(tempManifestPath);
                    }

                    return localManifest.data;
                }

                // Read downloaded server manifest
                var serverJson = await File.ReadAllTextAsync(tempManifestPath);
                var serverManifest = JsonSerializer.Deserialize<ManifestVersionReponse>(serverJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (serverManifest == null)
                {
                    _logger.LogError("Failed to deserialize server manifest for {AppCode}", appCode);

                    // Clean up temp file
                    if (File.Exists(tempManifestPath))
                    {
                        File.Delete(tempManifestPath);
                    }

                    return localManifest.data;
                }

                var serverVersion = serverManifest.data?.binary?.version ?? "0.0.0";

                _logger.LogInformation("Version comparison for {AppCode}: Local={LocalVersion}, Server={ServerVersion}",
                    appCode, localVersion, serverVersion);

                // Compare versions
                if (IsNewerVersion(serverVersion, localVersion))
                {
                    _logger.LogInformation("Server has newer version for {AppCode}. Replacing local manifest...", appCode);

                    // Replace old manifest with new one
                    File.Copy(tempManifestPath, manifestPath, overwrite: true);

                    // Clean up temp file
                    if (File.Exists(tempManifestPath))
                    {
                        File.Delete(tempManifestPath);
                    }

                    _logger.LogInformation("Manifest updated successfully for {AppCode} to version {Version}",
                        appCode, serverVersion);

                    return serverManifest.data;
                }
                else
                {
                    _logger.LogInformation("Local manifest for {AppCode} is up-to-date (version {Version})",
                        appCode, localVersion);

                    // Clean up temp file
                    if (File.Exists(tempManifestPath))
                    {
                        File.Delete(tempManifestPath);
                    }

                    return localManifest.data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking or updating manifest for {AppCode}", appCode);

                // Fallback: Try to return local manifest on error
                try
                {
                    var json = await File.ReadAllTextAsync(manifestPath);
                    return JsonSerializer.Deserialize<AppManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                    return null;
                }
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
        /// Download manifest.json file from server
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
                var downloadUrl = $"{_deploymentSettings.ServerBaseUrl}/api/apps/{appCode}/manifest/download";
                _logger.LogInformation("Downloading manifest from {Url} (Attempt {Attempt}/{MaxRetries})",
                    downloadUrl, retryCount + 1, maxRetries);

                var response = await _httpClient.GetAsync(downloadUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download manifest: {StatusCode}. Retry {Retry}/{MaxRetries}",
                        response.StatusCode, retryCount + 1, maxRetries);

                    if ((int)response.StatusCode >= 500 && retryCount + 1 < maxRetries)
                    {
                        await Task.Delay(1000 * (retryCount + 1));
                        return await DownloadManifestFileAsync(appCode, destinationPath, retryCount + 1, maxRetries);
                    }

                    _logger.LogError("Manifest not found or client error for {AppCode}: {StatusCode}",
                        appCode, response.StatusCode);
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Downloaded {Size} bytes for manifest.json", fileBytes.Length);

                // Validate JSON
                try
                {
                    var jsonString = System.Text.Encoding.UTF8.GetString(fileBytes);
                    var testManifest = JsonSerializer.Deserialize<ManifestVersionReponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (testManifest == null || string.IsNullOrEmpty(testManifest.data.appCode))
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

                await File.WriteAllBytesAsync(destinationPath, fileBytes);
                _logger.LogInformation("Manifest file saved to {Path}", destinationPath);

                return true;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error downloading manifest for {AppCode}", appCode);

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

        /// <summary>
        /// Compare version strings
        /// </summary>
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
                // Fallback to string comparison if not valid version format
                return serverVersion != localVersion;
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
                    package = $"{appCode}_v1.1.0.zip"
                },
                config = new ConfigInfo
                {
                    version = "0.0.1",
                    package = $"{appCode}_v1.1.0.zip",
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