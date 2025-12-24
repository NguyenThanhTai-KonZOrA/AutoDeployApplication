using ClientLauncher.Models;
using ClientLauncher.Models.Response;
using ClientLauncher.Services.Interface;
using NLog;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ClientLauncher.Services
{
    public class ManifestService : IManifestService
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private readonly string _appsBasePath;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ManifestService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _serverUrl = System.Configuration.ConfigurationManager.AppSettings["ServerUrl"] ?? "http://10.21.10.1:8102";
            _appsBasePath = System.Configuration.ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
        }

        public async Task<ManifestDto?> GetLocalManifestAsync(string appCode)
        {
            try
            {
                var manifestPath = Path.Combine(_appsBasePath, appCode, "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    _logger.Info($"Local manifest not found for {appCode}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.Info($"Loaded local manifest for {appCode}: v{manifest?.Binary?.Version}");
                return manifest;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading local manifest for {appCode}");
                return null;
            }
        }

        public async Task<ManifestDto?> DownloadManifestFromServerAsync(string appCode)
        {
            try
            {
                var downloadUrl = $"{_serverUrl}/api/apps/{appCode}/manifest/download";
                _logger.Info($"Downloading manifest from {downloadUrl}");

                var response = await _httpClient.GetAsync(downloadUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"Failed to download manifest: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                // Try deserialize with ApiBaseResponse wrapper first
                try
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiBaseResponse<ManifestDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _logger.Info($"Downloaded manifest for {appCode}: v{apiResponse.Data.Binary?.Version}");
                        return apiResponse.Data;
                    }
                }
                catch
                {
                    // Try direct deserialization
                    var manifest = JsonSerializer.Deserialize<ManifestDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (manifest != null)
                    {
                        _logger.Info($"Downloaded manifest (direct) for {appCode}: v{manifest.Binary?.Version}");
                        return manifest;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error downloading manifest for {appCode}");
                return null;
            }
        }

        public async Task SaveManifestAsync(string appCode, ManifestDto manifest)
        {
            try
            {
                var appFolder = Path.Combine(_appsBasePath, appCode);
                Directory.CreateDirectory(appFolder);

                var manifestPath = Path.Combine(appFolder, "manifest.json");
                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(manifestPath, json);
                _logger.Info($"Saved manifest for {appCode} to {manifestPath}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error saving manifest for {appCode}");
                throw;
            }
        }

        public string GetPackageName(ManifestDto manifest)
        {
            return manifest?.Binary?.Package ?? string.Empty;
        }
    }

    
}