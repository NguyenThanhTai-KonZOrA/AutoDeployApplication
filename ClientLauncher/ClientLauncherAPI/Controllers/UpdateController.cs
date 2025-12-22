using AppServer.API.Models;
using ClientLauncherAPI.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly ILogger<UpdateController> _logger;
        private readonly IUpdateService _updateService;
        public UpdateController(
            ILogger<UpdateController> logger,
            IUpdateService updateService
        )
        {
            _logger = logger;
            _updateService = updateService;
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckForUpdates([FromQuery] string appCode)
        {
            try
            {
                var updateApplied = await _updateService.CheckAndApplyUpdatesAsync(appCode);
                if (updateApplied)
                {
                    return Ok("Update applied");
                }
                else
                {
                    return Ok("No updates available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        //[HttpPost("update/binary")]
        //public async Task<IActionResult> ApplyBinaryUpdate([FromQuery] string appCode, [FromBody] AppManifest request)
        //{
        //    try
        //    {
        //        var updateApplied = await _updateService.appl(appCode, request);
        //        if (updateApplied)
        //        {
        //            return Ok("Binary update applied");
        //        }
        //        else
        //        {
        //            return Ok("No binary update available");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error applying binary update for {AppCode}", appCode);
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

        //[HttpPost("update/config")]
        //public async Task<IActionResult> ApplyConfigUpdate([FromQuery] string appCode)
        //{
        //    try
        //    {
        //        var updateApplied = await _updateService.ApplyConfigUpdateAsync(appCode);
        //        if (updateApplied)
        //        {
        //            return Ok("Config update applied");
        //        }
        //        else
        //        {
        //            return Ok("No config update available");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error applying config update for {AppCode}", appCode);
        //        return StatusCode(500, "Internal server error");
        //    }
        //}

    }
}
