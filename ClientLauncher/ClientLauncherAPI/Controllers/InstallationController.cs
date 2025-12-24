using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
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
        private readonly IUnitOfWork _unitOfWork;

        public InstallationController(
            IInstallationService installationService,
            ILogger<InstallationController> logger,
            IUnitOfWork unitOfWork)
        {
            _installationService = installationService;
            _logger = logger;
            _unitOfWork = unitOfWork;
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

        /// <summary>
        /// ✅ Receive installation logs from clients (DOES NOT INSTALL)
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> LogInstallation([FromBody] InstallationLogRequest request)
        {
            try
            {
                _logger.LogInformation("Received installation log from {Machine}/{User} for {AppCode}",
                    request.MachineName, request.UserName, request.AppCode);

                // Get application ID
                var app = await _unitOfWork.Applications.GetByAppCodeAsync(request.AppCode);

                if (app == null)
                {
                    _logger.LogWarning("Application {AppCode} not found", request.AppCode);
                    return NotFound($"Application {request.AppCode} not found");
                }

                var log = new InstallationLog
                {
                    ApplicationId = app.Id,
                    UserName = request.UserName,
                    MachineName = request.MachineName,
                    MachineId = $"{request.MachineName}_{request.UserName}",
                    Action = request.Action ?? "Install",
                    Status = request.Success ? "Success" : "Failed",
                    ErrorMessage = request.Error,
                    OldVersion = request.OldVersion ?? "0.0.0",
                    NewVersion = request.Version,
                    InstallationPath = $@"C:\CompanyApps\{request.AppCode}",
                    StartedAt = request.Timestamp.AddSeconds(-request.DurationSeconds),
                    CompletedAt = request.Timestamp,
                    DurationInSeconds = request.DurationSeconds
                };

                await _unitOfWork.InstallationLogs.AddAsync(log);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Installation log saved: {AppCode} {Action} {Status}",
                    request.AppCode, request.Action, log.Status);

                return Ok(new { message = "Log saved successfully", logId = log.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving installation log");
                return StatusCode(500, "Error saving installation log");
            }
        }
    }

    public class InstallationLogRequest
    {
        public string AppCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? OldVersion { get; set; }
        public string? Action { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime Timestamp { get; set; }
    }
}