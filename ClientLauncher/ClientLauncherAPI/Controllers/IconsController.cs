using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IconsController : ControllerBase
    {
        private readonly IIconsService _iconsService;
        private readonly ILogger<IconsController> _logger;
        private readonly IAuditLogService _auditLogService;

        public IconsController(IIconsService iconsService, ILogger<IconsController> logger, IAuditLogService auditLogService)
        {
            _iconsService = iconsService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Get all icons
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var icons = await _iconsService.GetAllAsync();
                return Ok(icons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icon by ID
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var icon = await _iconsService.GetByIdAsync(id);
                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                return Ok(icon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icons by type
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetByType(IconType type)
        {
            try
            {
                var icons = await _iconsService.GetByTypeAsync(type);
                return Ok(icons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icon by type and reference ID
        /// </summary>
        [HttpGet("type/{type}/reference/{referenceId}")]
        public async Task<IActionResult> GetByTypeAndReferenceId(IconType type, int referenceId)
        {
            try
            {
                var icon = await _iconsService.GetByTypeAndReferenceIdAsync(type, referenceId);
                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                return Ok(icon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new icon with file upload
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateIconRequest createRequest)
        {
            try
            {
                _logger.LogInformation("Creating new icon: {IconName}", createRequest.Name);
                var createdBy = User?.Identity?.Name ?? "System";
                var icon = await _iconsService.CreateAsync(createRequest, createdBy);

                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = createdBy,
                    Action = "Create Icon",
                    EntityType = nameof(Icons),
                    EntityId = icon.Id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = true,
                    StatusCode = 201,
                    Details = $"Icon '{icon.Name}' created with ID {icon.Id}",
                    ErrorMessage = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });

                return CreatedAtAction(nameof(GetById), new { id = icon.Id }, icon);
            }
            catch (ArgumentException ex)
            {
                // Audit log entry for failure
                var createdBy = User?.Identity?.Name ?? "System";
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = createdBy,
                    Action = "Create Icon",
                    EntityType = nameof(Icons),
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 400,
                    Details = null,
                    ErrorMessage = ex.Message,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Audit log entry for failure
                var createdBy = User?.Identity?.Name ?? "System";
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = createdBy,
                    Action = "Create Icon",
                    EntityType = nameof(Icons),
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 500,
                    Details = null,
                    ErrorMessage = ex.Message,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Update icon information
        /// </summary>
        [HttpPost("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateIconRequest updateDto)
        {
            try
            {
                _logger.LogInformation("Updating icon ID: {IconId}", id);
                var updatedBy = User?.Identity?.Name ?? "System";
                var icon = await _iconsService.UpdateAsync(id, updateDto, updatedBy);

                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = updatedBy,
                    Action = "Update Icon",
                    EntityType = nameof(Icons),
                    EntityId = icon.Id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = true,
                    StatusCode = 200,
                    Details = $"Icon '{icon.Name}' updated with ID {icon.Id}",
                    ErrorMessage = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });

                return Ok(icon);
            }
            catch (ArgumentException ex)
            {
                // Audit log entry for failure
                var updatedBy = User?.Identity?.Name ?? "System";
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = updatedBy,
                    Action = "Update Icon",
                    EntityType = nameof(Icons),
                    EntityId = id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 400,
                    Details = null,
                    ErrorMessage = ex.Message,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Audit log entry for failure
                var updatedBy = User?.Identity?.Name ?? "System";
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = updatedBy,
                    Action = "Update Icon",
                    EntityType = nameof(Icons),
                    EntityId = id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 500,
                    Details = null,
                    ErrorMessage = ex.Message,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Upload/Replace icon file
        /// </summary>
        //[HttpPost("upload/{id}")]
        //public async Task<IActionResult> UploadIcon(int id, [FromForm] IFormFile file)
        //{
        //    try
        //    {
        //        var updatedBy = User?.Identity?.Name ?? "System";
        //        var icon = await _iconsService.UploadIconAsync(id, file, updatedBy);

        //        if (icon == null)
        //            return NotFound(new { message = "Icon not found" });

        //        return Ok(icon);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //    }
        //}

        /// <summary>
        /// Delete icon (soft delete)
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting icon ID: {IconId}", id);
                var deletedBy = User?.Identity?.Name ?? "System";
                var result = await _iconsService.DeleteAsync(id, deletedBy);

                if (!result)
                    return NotFound(new { message = "Icon not found" });

                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = deletedBy,
                    Action = "Delete Icon",
                    EntityType = nameof(Icons),
                    EntityId = id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = true,
                    StatusCode = 200,
                    Details = $"Icon with ID {id} deleted",
                    ErrorMessage = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Audit log entry for failure
                var deletedBy = User?.Identity?.Name ?? "System";
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    UserName = deletedBy,
                    Action = "Delete Icon",
                    EntityType = nameof(Icons),
                    EntityId = id,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 500,
                    Details = null,
                    ErrorMessage = ex.Message,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    DurationMs = null
                });
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}