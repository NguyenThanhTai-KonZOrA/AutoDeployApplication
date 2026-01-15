using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeRoleController : ControllerBase
    {
        private readonly IEmployeeRoleService _employeeRoleService;
        private readonly ILogger<EmployeeRoleController> _logger;
        private readonly IAuditLogService _auditLogService;

        public EmployeeRoleController(IEmployeeRoleService employeeRoleService, ILogger<EmployeeRoleController> logger, IAuditLogService auditLogService)
        {
            _employeeRoleService = employeeRoleService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Get employee with their roles and permissions
        /// </summary>
        [HttpGet("roles/{employeeId}")]
        public async Task<IActionResult> GetEmployeeWithRoles(int employeeId)
        {
            try
            {
                _logger.LogInformation("[GetEmployeeWithRoles]: Retrieving employee {EmployeeId} with roles", employeeId);
                var result = await _employeeRoleService.GetEmployeeWithRolesAsync(employeeId);

                if (result == null)
                {
                    _logger.LogWarning("[GetEmployeeWithRoles]: Employee {EmployeeId} not found", employeeId);
                    return NotFound(new { message = $"Employee with ID {employeeId} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetEmployeeWithRoles]: Error retrieving employee {EmployeeId}", employeeId);
                return StatusCode(500, new { message = "An error occurred while retrieving employee roles", error = ex.Message });
            }
        }

        /// <summary>
        /// Assign roles to an employee
        /// </summary>
        [HttpPost("assign-roles")]
        public async Task<IActionResult> AssignRolesToEmployee([FromBody] AssignRoleToEmployeeRequest request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                _logger.LogInformation("[AssignRolesToEmployee]: Assigning {Count} roles to employee {EmployeeId} by {User}",
                    request.RoleIds.Count, request.EmployeeId, userName);

                var result = await _employeeRoleService.AssignRolesToEmployeeAsync(request, userName);

                if (result)
                {
                    // Audit log entry
                    await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                    {
                        EntityId = request.EmployeeId,
                        EntityType = "Employee",
                        UserName = userName,
                        HttpMethod = "POST",
                        RequestPath = HttpContext.Request.Path,
                        IsSuccess = true,
                        StatusCode = 200,
                        ErrorMessage = null,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                        Action = "AssignRoles",
                        DurationMs = null,
                        Details = $"Assigned roles [{string.Join(", ", request.RoleIds)}] to employee ID {request.EmployeeId} by {userName}"
                    });

                    _logger.LogInformation("[AssignRolesToEmployee]: Roles assigned successfully to employee {EmployeeId}", request.EmployeeId);
                    return Ok(new { message = "Roles assigned successfully", success = true });
                }

                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    EntityId = request.EmployeeId,
                    EntityType = "Employee",
                    UserName = userName,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "Failed to assign roles",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Action = "AssignRoles",
                    DurationMs = null,
                    Details = $"Failed to assign roles [{string.Join(", ", request.RoleIds)}] to employee ID {request.EmployeeId} by {userName}"
                });

                return BadRequest(new { message = "Failed to assign roles", success = false });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "[AssignRolesToEmployee]: Validation error assigning roles to employee {EmployeeId}", request.EmployeeId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                string userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    EntityId = request.EmployeeId,
                    EntityType = "Employee",
                    UserName = userName,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    IsSuccess = false,
                    StatusCode = 400,
                    ErrorMessage = "Failed to assign roles",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Action = "AssignRoles",
                    DurationMs = null,
                    Details = $"Failed to assign roles [{string.Join(", ", request.RoleIds)}] to employee ID {request.EmployeeId} by {userName}"
                });
                _logger.LogError(ex, "[AssignRolesToEmployee]: Error assigning roles to employee {EmployeeId}", request.EmployeeId);
                return StatusCode(500, new { message = "An error occurred while assigning roles", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all permissions for an employee (from all their roles)
        /// </summary>
        [HttpGet("permissions/{employeeId}")]
        public async Task<IActionResult> GetEmployeePermissions(int employeeId)
        {
            try
            {
                _logger.LogInformation("[GetEmployeePermissions]: Retrieving permissions for employee {EmployeeId}", employeeId);
                var result = await _employeeRoleService.GetEmployeePermissionsAsync(employeeId);
                return Ok(new { employeeId, permissions = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetEmployeePermissions]: Error retrieving permissions for employee {EmployeeId}", employeeId);
                return StatusCode(500, new { message = "An error occurred while retrieving employee permissions", error = ex.Message });
            }
        }
    }
}