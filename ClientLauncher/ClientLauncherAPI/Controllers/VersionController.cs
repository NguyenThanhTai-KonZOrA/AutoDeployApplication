using ClientLauncher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;
        private readonly IVersionService _versionService;
        public VersionController(
            ILogger<VersionController> logger,
            IVersionService versionService
            )
        {
            _logger = logger;
            _versionService = versionService;
        }

        [HttpGet("version")]
        public async Task<IActionResult> GetVersion([FromQuery] string appCode)
        {
            try
            {
                var localVersions = _versionService.GetLocalVersions(appCode);
                return Ok(localVersions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving versions for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("version/binary")]
        public async Task<IActionResult> GetBinaryVersion([FromQuery] string appCode)
        {
            try
            {
                var localVersions = _versionService.GetLocalVersions(appCode);
                return Ok(localVersions.BinaryVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving binary version for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("version/config")]
        public async Task<IActionResult> GetConfigVersion([FromQuery] string appCode)
        {
            try
            {
                var localVersions = _versionService.GetLocalVersions(appCode);
                return Ok(localVersions.ConfigVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving config version for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("version/isNewer")]
        public async Task<IActionResult> IsNewerVersion([FromQuery] string serverVersion, [FromQuery] string localVersion)
        {
            try
            {
                var isNewer = _versionService.IsNewerVersion(serverVersion, localVersion);
                return Ok(isNewer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing versions: ServerVersion={ServerVersion}, LocalVersion={LocalVersion}", serverVersion, localVersion);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}