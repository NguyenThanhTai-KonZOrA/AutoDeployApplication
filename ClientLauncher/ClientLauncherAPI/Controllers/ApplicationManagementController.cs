using ClientLancher.Common.Constants;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationManagementController : ControllerBase
    {
        private readonly IApplicationManagementService _appService;
        private readonly ILogger<ApplicationManagementController> _logger;
        private readonly IManifestManagementService _manifestService;
        private readonly IAuditLogService _auditLogService;

        public ApplicationManagementController(
            IApplicationManagementService appService,
            IManifestManagementService manifestService,
            ILogger<ApplicationManagementController> logger,
            IAuditLogService auditLogService)
        {
            _appService = appService;
            _manifestService = manifestService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Create a new application
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateApplication([FromBody] ApplicationCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new application with code: {AppCode}", request.AppCode);
                var result = await _appService.CreateApplicationAsync(request);

                // Log audit entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "CreateApplication",
                    EntityType = "Application",
                    Details = $"Created application with ID: {result.Id}",
                    ErrorMessage = null,
                    IsSuccess = true,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = result.Id
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing application
        /// </summary>
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromBody] ApplicationUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating application ID: {Id}", id);
                var result = await _appService.UpdateApplicationAsync(id, request);

                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "UpdateApplication",
                    EntityType = "Application",
                    Details = $"Updated application ID: {id}",
                    ErrorMessage = null,
                    IsSuccess = true,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = id
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete an application
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                _logger.LogInformation("Deleting application ID: {Id}", id);
                var result = await _appService.DeleteApplicationAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Application ID: {Id} not found for deletion", id);
                    return NotFound(new { success = false, message = "Application not found" });
                }

                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "DeleteApplication",
                    EntityType = "Application",
                    Details = $"Deleted application ID: {id}",
                    ErrorMessage = null,
                    IsSuccess = true,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = id
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application ID: {Id}", id);
                // Audit log for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "DeleteApplication",
                    EntityType = "Application",
                    Details = $"Failed to delete application ID: {id}",
                    ErrorMessage = ex.Message,
                    IsSuccess = false,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 400,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = id
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("change-status/{id}")]
        public async Task<IActionResult> ChangeApplicationStatus(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to change status for application ID: {Id}", id);
                var result = await _appService.ChangeApplicationStatusAsync(id);

                // Log audit entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "ChangeApplicationStatus",
                    EntityType = "Application",
                    Details = $"Changed status for application ID: {id}",
                    ErrorMessage = null,
                    IsSuccess = true,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = id
                });
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing status for application ID: {Id}", id);
                // Log audit entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "ChangeApplicationStatus",
                    EntityType = "Application",
                    Details = $"Failed to change status for application ID: {id}",
                    ErrorMessage = ex.Message,
                    IsSuccess = false,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 400,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = id
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get application by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetApplication(int id)
        {
            try
            {
                var result = await _appService.GetApplicationByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Application not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get application by code
        /// </summary>
        [HttpGet("code/{appCode}")]
        public async Task<IActionResult> GetApplicationByCode(string appCode)
        {
            try
            {
                var result = await _appService.GetApplicationByCodeAsync(appCode);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Application not found" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application by code: {AppCode}", appCode);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all applications
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var result = await _appService.GetAllApplicationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all applications");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get applications by category
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetApplicationsByCategory(int categoryId)
        {
            try
            {
                var result = await _appService.GetApplicationsByCategoryAsync(categoryId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications by category ID: {CategoryId}", categoryId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get application with statistics
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetApplicationWithStats(int id)
        {
            try
            {
                var result = await _appService.GetApplicationWithStatsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application stats for ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #region Manifest Management

        /// <summary>
        /// Create new manifest version for application
        /// </summary>
        [HttpPost("{id}/manifest")]
        public async Task<IActionResult> CreateManifest(int id, [FromBody] ManifestCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating manifest for application ID: {Id}", id);
                var result = await _manifestService.CreateManifestAsync(id, request, User.Identity?.Name ?? CommonConstants.SystemUser);

                // Log audit entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "CreateManifest",
                    EntityType = "Manifest",
                    Details = $"Created manifest ID: {result.Id} for application ID: {id}",
                    ErrorMessage = null,
                    IsSuccess = true,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = result.Id
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manifest for application ID: {Id}", id);
                // Log audit entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = User.Identity?.Name ?? CommonConstants.SystemUser,
                    Action = "CreateManifest",
                    EntityType = "Manifest",
                    Details = $"Failed to create manifest for application ID: {id}",
                    ErrorMessage = ex.Message,
                    IsSuccess = false,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    StatusCode = 400,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = 0,
                    EntityId = null
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update manifest
        /// </summary>
        [HttpPost("update/{id}/manifest/{manifestId}")]
        public async Task<IActionResult> UpdateManifest(int id, int manifestId, [FromBody] ManifestUpdateRequest request)
        {
            try
            {
                var result = await _manifestService.UpdateManifestAsync(manifestId, request, User.Identity?.Name ?? CommonConstants.SystemUser);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manifest ID: {ManifestId}", manifestId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get specific manifest by ID
        /// </summary>
        [HttpGet("{id}/manifest/{manifestId}")]
        public async Task<IActionResult> GetManifest(int id, int manifestId)
        {
            try
            {
                var result = await _manifestService.GetManifestByIdAsync(manifestId);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Manifest not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manifest ID: {ManifestId}", manifestId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get latest active manifest for application
        /// </summary>
        [HttpGet("{id}/manifest/latest")]
        public async Task<IActionResult> GetLatestManifest(int id)
        {
            try
            {
                var result = await _manifestService.GetLatestManifestAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "No active manifest found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest manifest for application ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get manifest history for application
        /// </summary>
        [HttpGet("{id}/manifest/history")]
        public async Task<IActionResult> GetManifestHistory(int id, [FromQuery] int take = 10)
        {
            try
            {
                var result = await _manifestService.GetManifestHistoryAsync(id, take);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manifest history for application ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Activate specific manifest version
        /// </summary>
        [HttpPost("{id}/manifest/{manifestId}/activate")]
        public async Task<IActionResult> ActivateManifest(int id, int manifestId)
        {
            try
            {
                var result = await _manifestService.ActivateManifestAsync(manifestId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Manifest not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating manifest ID: {ManifestId}", manifestId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate specific manifest version
        /// </summary>
        [HttpPost("{id}/manifest/{manifestId}/deactivate")]
        public async Task<IActionResult> DeactivateManifest(int id, int manifestId)
        {
            try
            {
                var result = await _manifestService.DeactivateManifestAsync(manifestId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Manifest not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating manifest ID: {ManifestId}", manifestId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete manifest
        /// </summary>
        [HttpDelete("{id}/manifest/{manifestId}")]
        public async Task<IActionResult> DeleteManifest(int id, int manifestId)
        {
            try
            {
                var result = await _manifestService.DeleteManifestAsync(manifestId);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Manifest not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting manifest ID: {ManifestId}", manifestId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion
    }
}