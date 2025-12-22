using AppServer.API.Models;
using ClientLauncherAPI.Services.Interface;
using System.Text.Json;

namespace ClientLauncherAPI.Services
{
    public class ManifestService : IManifestService
    {
        private readonly string _appsBasePath;
        private readonly ILogger<ManifestService> _logger;

        public ManifestService(IConfiguration configuration, ILogger<ManifestService> logger)
        {
            _appsBasePath = configuration["AppStorage:BasePath"] ?? "wwwroot/apps";
            _logger = logger;
        }

        public async Task<AppManifest?> GetManifestAsync(string appCode)
        {
            var manifestPath = Path.Combine(_appsBasePath, appCode, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest not found at {Path}", manifestPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<AppManifest>(json);
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
            _logger.LogInformation("Manifest updated for {AppCode}", appCode);
        }
    }
}
