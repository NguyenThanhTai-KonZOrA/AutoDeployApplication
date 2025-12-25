using ClientLancher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/apps")]
    public class ManifestController : ControllerBase
    {
        private readonly IManifestManagementService _manifestService;
        private readonly ILogger<ManifestController> _logger;

        public ManifestController(
            IManifestManagementService manifestService,
            ILogger<ManifestController> logger)
        {
            _manifestService = manifestService;
            _logger = logger;
        }

        /// <summary>
        /// Get manifest as JSON object (Generated from database)
        /// </summary>
        [HttpGet("{appCode}/manifest")]
        public async Task<IActionResult> GetManifest(string appCode)
        {
            try
            {
                var manifest = await _manifestService.GenerateManifestJsonAsync(appCode);
                if (manifest == null)
                {
                    return NotFound(new { success = false, message = $"No active manifest found for app: {appCode}" });
                }

                return Ok(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manifest for {AppCode}", appCode);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get only version info from manifest
        /// </summary>
        [HttpGet("{appCode}/version")]
        public async Task<IActionResult> GetManifestVersion(string appCode)
        {
            try
            {
                var manifest = await _manifestService.GetLatestManifestByAppCodeAsync(appCode);
                if (manifest == null)
                {
                    return NotFound(new { success = false, message = $"Manifest not found for app: {appCode}" });
                }

                var versionInfo = new
                {
                    appCode = manifest.AppCode,
                    binaryVersion = manifest.BinaryVersion,
                    configVersion = manifest.ConfigVersion ?? manifest.BinaryVersion,
                    updateType = manifest.UpdateType,
                    forceUpdate = manifest.ForceUpdate
                };

                return Ok(versionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manifest version for {AppCode}", appCode);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Download manifest.json file (Generated dynamically from database)
        /// </summary>
        [HttpGet("{appCode}/manifest/download")]
        public async Task<IActionResult> DownloadManifest(string appCode)
        {
            try
            {
                _logger.LogInformation("Download manifest request for {AppCode}", appCode);

                if (string.IsNullOrWhiteSpace(appCode) || appCode.Contains("..") ||
                    appCode.Contains("/") || appCode.Contains("\\"))
                {
                    _logger.LogWarning("Invalid appCode detected: {AppCode}", appCode);
                    return BadRequest(new { success = false, message = "Invalid application code" });
                }

                var manifest = await _manifestService.GenerateManifestJsonAsync(appCode);
                if (manifest == null)
                {
                    _logger.LogWarning("No active manifest found for {AppCode}", appCode);
                    return NotFound(new { success = false, message = $"Manifest not found for app: {appCode}" });
                }

                // Convert to JSON
                var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
                _logger.LogInformation("Generated {Size} bytes manifest.json for {AppCode}",
                    fileBytes.Length, appCode);

                return File(fileBytes, "application/json", "manifest.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading manifest for {AppCode}", appCode);
                return StatusCode(500, new { success = false, message = "Error downloading manifest" });
            }
        }
    }
}