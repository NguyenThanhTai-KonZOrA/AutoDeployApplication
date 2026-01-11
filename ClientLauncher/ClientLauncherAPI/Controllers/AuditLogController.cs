using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated logs with multiple filter conditions
        /// Supports filtering by: UserName, Action, EntityType, IsSuccess, FromDate, ToDate
        /// </summary>
        [HttpPost("paginate")]
        public async Task<IActionResult> GetPaginatedLogs([FromBody] AuditLogPaginationRequest request)
        {
            try
            {
                _logger.LogInformation("[GetPaginatedLogs] START - Request: {@Request}", request);
                var result = await _auditLogService.GetPaginatedLogsAsync(request);
                _logger.LogInformation("[GetPaginatedLogs] END - Retrieved {Count} logs", result.Logs.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPaginatedLogs] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Write a new audit log entry
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> LogAction([FromBody] CreateAuditLogRequest request)
        {
            try
            {
                _logger.LogInformation("[LogAction] START - Action: {Action}, User: {UserName}", request.Action, request.UserName);
                await _auditLogService.LogActionAsync(request);
                _logger.LogInformation("[LogAction] END - Successfully logged action");
                return Ok(new { success = true, message = "Action logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogAction] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific audit log by ID
        /// </summary>
        [HttpGet("{auditLogId}")]
        public async Task<IActionResult> GetLogById(int auditLogId)
        {
            try
            {
                _logger.LogInformation("[GetLogById] START - ID: {Id}", auditLogId);
                var log = await _auditLogService.GetByIdAsync(auditLogId);

                if (log == null)
                {
                    _logger.LogWarning("[GetLogById] NOT FOUND - ID: {Id}", auditLogId);
                    return NotFound(new { success = false, message = "Audit log not found" });
                }

                _logger.LogInformation("[GetLogById] END - Found log ID: {Id}", auditLogId);
                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLogById] FAILED - ID: {Id}", auditLogId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Legacy APIs (kept for backward compatibility)

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _auditLogService.GetAllAsync(page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetAllLogs] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-user/{userName}")]
        public async Task<IActionResult> GetLogsByUser(string userName, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _auditLogService.GetByUserNameAsync(userName, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLogsByUser] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-action/{action}")]
        public async Task<IActionResult> GetLogsByAction(string action, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _auditLogService.GetByActionAsync(action, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLogsByAction] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _auditLogService.GetFailedLogsAsync(page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetFailedLogs] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-date-range")]
        public async Task<IActionResult> GetLogsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _auditLogService.GetByDateRangeAsync(startDate, endDate, page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetLogsByDateRange] FAILED");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        #endregion
    }
}