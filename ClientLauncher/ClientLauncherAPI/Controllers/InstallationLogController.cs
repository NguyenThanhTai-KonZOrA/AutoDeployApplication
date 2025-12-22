using ClientLancher.Implement.UnitOfWork;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstallationLogController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InstallationLogController> _logger;

        public InstallationLogController(
            IUnitOfWork unitOfWork,
            ILogger<InstallationLogController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetByApplicationId(int applicationId)
        {
            try
            {
                var logs = await _unitOfWork.InstallationLogs.GetByApplicationIdAsync(applicationId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("user/{userName}")]
        public async Task<IActionResult> GetByUserName(string userName)
        {
            try
            {
                var logs = await _unitOfWork.InstallationLogs.GetByUserNameAsync(userName);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs for user {UserName}", userName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("machine/{machineName}")]
        public async Task<IActionResult> GetByMachineName(string machineName)
        {
            try
            {
                var logs = await _unitOfWork.InstallationLogs.GetByMachineNameAsync(machineName);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs for machine {MachineName}", machineName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedInstallations()
        {
            try
            {
                var logs = await _unitOfWork.InstallationLogs.GetFailedInstallationsAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching failed installations");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("history/{appCode}")]
        public async Task<IActionResult> GetInstallationHistory(string appCode, [FromQuery] int take = 10)
        {
            try
            {
                var logs = await _unitOfWork.InstallationLogs.GetInstallationHistoryAsync(appCode, take);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching installation history for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}