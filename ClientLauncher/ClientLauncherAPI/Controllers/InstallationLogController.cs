using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstallationLogController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InstallationLogController> _logger;
        private readonly IInstallationLogService _installationLogService;

        public InstallationLogController(
            IUnitOfWork unitOfWork,
            ILogger<InstallationLogController> logger,
            IInstallationLogService installationLogService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _installationLogService = installationLogService;
        }

        [HttpPost("logs")]
        public async Task<IActionResult> GetInstallationLogs([FromBody] InstallationLogFilterRequest request)
        {
            try
            {
                var logs = await _installationLogService.GetInstallationLogByFilterAsync(request);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching installation logs");
                return StatusCode(500, "Internal server error");
            }
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

        /// <summary>
        /// Get installation report grouped by version
        /// Shows how many PCs have each version installed
        /// </summary>
        [HttpPost("report/by-version")]
        public async Task<IActionResult> GetInstallationReportByVersion([FromBody] InstallationReportRequest request)
        {
            try
            {
                var report = await _installationLogService.GetInstallationReportByVersionAsync(request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating installation report by version");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }
}