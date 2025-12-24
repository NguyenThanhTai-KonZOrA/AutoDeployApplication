using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace AppServer.API.Controllers
{
    [ApiController]
    [Route("api/apps")]
    public class ManifestController : ControllerBase
    {
        private readonly IServerManifestService _manifestService;
        private readonly ILogger<ManifestController> _logger;
        private readonly string _manifestsBasePath;
        private readonly IWebHostEnvironment _environment;

        public ManifestController(
            IServerManifestService manifestService,
            ILogger<ManifestController> logger,
            IWebHostEnvironment environment)
        {
            _manifestService = manifestService;
            _logger = logger;
            _environment = environment;
            _manifestsBasePath = Path.Combine(_environment.ContentRootPath, "Manifests");
        }

        /// <summary>
        /// Get manifest as JSON object
        /// </summary>
        [HttpGet("{appCode}/manifest")]
        public async Task<ActionResult<AppManifest>> GetManifest(string appCode)
        {
            try
            {
                var manifest = await _manifestService.GetManifestAsync(appCode);
                if (manifest == null)
                {
                    return NotFound($"Manifest not found for app: {appCode}");
                }
                return Ok(manifest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manifest for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get only version info from server manifest
        /// </summary>
        [HttpGet("{appCode}/version")]
        public async Task<IActionResult> GetManifestVersion(string appCode)
        {
            try
            {
                var manifest = await _manifestService.GetManifestAsync(appCode);
                if (manifest == null)
                {
                    return NotFound($"Manifest not found for app: {appCode}");
                }

                var versionInfo = new
                {
                    appCode = manifest.appCode,
                    binaryVersion = manifest.binary?.version ?? "0.0.0",
                    configVersion = manifest.config?.version ?? "0.0.0",
                    updateType = manifest.updatePolicy?.type ?? "none",
                    forceUpdate = manifest.updatePolicy?.force ?? false
                };

                return Ok(versionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manifest version for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Download manifest.json file
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
                    return BadRequest("Invalid application code");
                }

                var manifestPath = Path.Combine(_manifestsBasePath, appCode, "manifest.json");

                _logger.LogInformation("Looking for manifest at: {Path}", manifestPath);

                if (!System.IO.File.Exists(manifestPath))
                {
                    _logger.LogWarning("Manifest file not found: {Path}", manifestPath);
                    return NotFound($"Manifest file not found for app: {appCode}");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(manifestPath);
                _logger.LogInformation("Successfully read {Size} bytes from manifest.json for {AppCode}",
                    fileBytes.Length, appCode);

                return File(fileBytes, "application/json", "manifest.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading manifest for {AppCode}", appCode);
                return StatusCode(500, "Error downloading manifest");
            }
        }

        [HttpPost("{appCode}/manifest")]
        public async Task<IActionResult> UpdateManifest(string appCode, [FromBody] AppManifest manifest)
        {
            try
            {
                await _manifestService.UpdateManifestAsync(appCode, manifest);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manifest for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}