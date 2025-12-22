using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstallationController : ControllerBase
    {
        private readonly IInstallationService _installationService;
        private readonly ILogger<InstallationController> _logger;

        public InstallationController(
            IInstallationService installationService,
            ILogger<InstallationController> logger)
        {
            _installationService = installationService;
            _logger = logger;
        }

        [HttpPost("install")]
        public async Task<IActionResult> InstallApplication([FromBody] InstallationRequest request)
        {
            try
            {
                var result = await _installationService.InstallApplicationAsync(
                    request.AppCode,
                    request.UserName);

                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing application {AppCode}", request.AppCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateApplication([FromBody] InstallationRequest request)
        {
            try
            {
                var result = await _installationService.UpdateApplicationAsync(
                    request.AppCode,
                    request.UserName);

                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {AppCode}", request.AppCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("uninstall")]
        public async Task<IActionResult> UninstallApplication([FromBody] InstallationRequest request)
        {
            try
            {
                var result = await _installationService.UninstallApplicationAsync(
                    request.AppCode,
                    request.UserName);

                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uninstalling application {AppCode}", request.AppCode);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}