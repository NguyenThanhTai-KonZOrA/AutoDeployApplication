using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;

namespace ClientLauncher.Implement.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly IVersionService _versionService;
        private readonly ILogger<UpdateService> _logger;
        private readonly DeploymentSettings _deploymentSettings;

        public UpdateService(HttpClient httpClient, IVersionService versionService, ILogger<UpdateService> logger, DeploymentSettings deploymentSettings)
        {
            _httpClient = httpClient;
            _versionService = versionService;
            _logger = logger;
            _deploymentSettings = deploymentSettings;
        }

        public async Task<bool> CheckAndApplyUpdatesAsync(string appCode)
        {
            try
            {
                _logger.LogInformation($"Checking updates for {appCode}");

                // Fetch manifest
                var manifestUrl = $"{_deploymentSettings.ServerBaseUrl}/api/apps/{appCode}/manifest";
                var response = await _httpClient.GetAsync(manifestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Failed to fetch manifest: {response.StatusCode}");
                    return false;
                }

                var manifestJson = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<AppManifest>(manifestJson);

                if (manifest == null)
                {
                    _logger.LogInformation("Invalid manifest");
                    return false;
                }

                var localInfo = _versionService.GetLocalVersions(appCode);
                bool updateApplied = false;

                // Check binary update
                if (manifest.updatePolicy.type is "both" or "binary")
                {
                    if (_versionService.IsNewerVersion(manifest.binary.version, localInfo.BinaryVersion))
                    {
                        _logger.LogInformation($"Binary update available: {localInfo.BinaryVersion} -> {manifest.binary.version}");
                        await ApplyBinaryUpdateAsync(appCode, manifest);
                        updateApplied = true;
                    }
                }

                // Check config update
                if (manifest.updatePolicy.type is "both" or "config")
                {
                    if (_versionService.IsNewerVersion(manifest.config.version, localInfo.ConfigVersion))
                    {
                        _logger.LogInformation($"Config update available: {localInfo.ConfigVersion} -> {manifest.config.version}");
                        await ApplyConfigUpdateAsync(appCode, manifest);
                        updateApplied = true;
                    }
                }

                return updateApplied;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error checking updates: {ex.Message}");
                return false;
            }
        }

        private async Task ApplyBinaryUpdateAsync(string appCode, AppManifest manifest)
        {
            var packageUrl = $"{_deploymentSettings.ServerBaseUrl}/apps/{appCode}/{manifest.binary.package}";
            var appPath = $@"C:\CompanyApps\{appCode}\App";
            var tempZip = Path.Combine(Path.GetTempPath(), $"{appCode}_update.zip");

            try
            {
                // Download package
                _logger.LogInformation($"Downloading binary from {packageUrl}");
                var packageData = await _httpClient.GetByteArrayAsync(packageUrl);
                await File.WriteAllBytesAsync(tempZip, packageData);

                // Extract to app folder
                _logger.LogInformation($"Extracting to {appPath}");
                if (Directory.Exists(appPath))
                {
                    Directory.Delete(appPath, true);
                }
                Directory.CreateDirectory(appPath);
                ZipFile.ExtractToDirectory(tempZip, appPath);

                // Update version file
                _versionService.SaveBinaryVersion(appCode, manifest.binary.version);
                _logger.LogInformation("Binary update completed");
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }
            }
        }

        private async Task ApplyConfigUpdateAsync(string appCode, AppManifest manifest)
        {
            var packageUrl = $"{_deploymentSettings.ServerBaseUrl}/apps/{appCode}/{manifest.config.package}";
            var configPath = $@"C:\CompanyApps\{appCode}\Config\config.json";

            try
            {
                _logger.LogInformation($"Downloading config from {packageUrl}");
                var configJson = await _httpClient.GetStringAsync(packageUrl);

                if (manifest.config.mergeStrategy == "replaceAll")
                {
                    await File.WriteAllTextAsync(configPath, configJson);
                }
                else // preserveLocal
                {
                    // Merge logic here
                    await File.WriteAllTextAsync(configPath, configJson);
                }

                _versionService.SaveConfigVersion(appCode, manifest.config.version);
                _logger.LogInformation("Config update completed");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Config update failed: {ex.Message}");
            }
        }
    }
}