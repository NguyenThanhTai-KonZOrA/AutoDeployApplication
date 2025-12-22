using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace AppServer.API.Controllers
{
    [ApiController]
    [Route("api/apps")]
    public class ManifestController : ControllerBase
    {
        private readonly IManifestService _manifestService;
        private readonly ILogger<ManifestController> _logger;

        public ManifestController(IManifestService manifestService, ILogger<ManifestController> logger)
        {
            _manifestService = manifestService;
            _logger = logger;
        }

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