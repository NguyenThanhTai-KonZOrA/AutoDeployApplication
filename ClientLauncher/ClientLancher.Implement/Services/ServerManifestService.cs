using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClientLauncher.Implement.Services
{
    public class ServerManifestService : IServerManifestService
    {
        private readonly string _manifestsBasePath;
        private readonly ILogger<ServerManifestService> _logger;

        public ServerManifestService(Microsoft.AspNetCore.Hosting.IHostingEnvironment environment, ILogger<ServerManifestService> logger)
        {
            _logger = logger;
            // Use server's local storage (relative to application root)
            _manifestsBasePath = Path.Combine(environment.ContentRootPath, "Manifests");

            // Ensure directory exists
            Directory.CreateDirectory(_manifestsBasePath);

            _logger.LogInformation("Server manifest storage initialized at: {Path}", _manifestsBasePath);
        }

        public async Task<AppManifest?> GetManifestAsync(string appCode)
        {
            try
            {
                var manifestPath = Path.Combine(_manifestsBasePath, appCode, "manifest.json");

                if (!File.Exists(manifestPath))
                {
                    _logger.LogWarning("Manifest not found for {AppCode} at {Path}", appCode, manifestPath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<AppManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return manifest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading manifest for {AppCode}", appCode);
                return null;
            }
        }

        public async Task UpdateManifestAsync(string appCode, AppManifest manifest)
        {
            try
            {
                var appFolder = Path.Combine(_manifestsBasePath, appCode);
                Directory.CreateDirectory(appFolder);

                var manifestPath = Path.Combine(appFolder, "manifest.json");
                var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(manifestPath, json);
                _logger.LogInformation("Manifest updated for {AppCode} at {Path}", appCode, manifestPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manifest for {AppCode}", appCode);
                throw;
            }
        }
    }
}