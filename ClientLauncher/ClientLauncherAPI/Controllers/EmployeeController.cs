using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;

        public EmployeeController(
            ILogger<EmployeeController> logger,
            IEmployeeService employeeService,
            IAuditLogService auditLogService)
        {
            _logger = logger;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetEmployeeListAsync()
        {
            try
            {
                _logger.LogInformation("[GetEmployeeListAsync]: called");
                var employees = await _employeeService.GetActiveEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetEmployeeListAsync]: Error occurred while retrieving employee list");
                return StatusCode(500, "Internal server error while retrieving employees.");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateEmployeeAsync([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                _logger.LogInformation("[CreateEmployeeAsync]: Creating employee {EmployeeName}", request.EmployeeName);
                var newEmployee = await _employeeService.GetOrCreateDefaultEmployeeAsync(request.EmployeeUserName);

                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "CreateEmployee",
                    Details = $"Created employee with username {request.EmployeeUserName}",
                    DurationMs = 0,
                    EntityId = newEmployee.Id,
                    EntityType = "Employee",
                    ErrorMessage = null,
                    HttpMethod = "POST",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IsSuccess = true,
                    RequestPath = HttpContext.Request.Path,
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser
                });

                return Ok(newEmployee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreateEmployeeAsync]: Error occurred while creating employee {EmployeeName}", request.EmployeeName);
                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "CreateEmployee",
                    Details = $"Failed to create employee with username {request.EmployeeUserName}",
                    DurationMs = 0,
                    EntityType = "Employee",
                    ErrorMessage = ex.Message,
                    HttpMethod = "POST",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IsSuccess = false,
                    RequestPath = HttpContext.Request.Path,
                    StatusCode = 500,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser
                });
                return StatusCode(500, "Internal server error while creating employee.");
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateEmployeeAsync(int id, [FromBody] CreateEmployeeRequest request)
        {
            try
            {
                _logger.LogInformation("[CreateEmployeeAsync]: Creating employee {EmployeeName}", request.EmployeeName);
                var newEmployeeId = await _employeeService.GetOrCreateDefaultEmployeeAsync(request.EmployeeUserName);
                return Ok(new { EmployeeId = newEmployeeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreateEmployeeAsync]: Error occurred while creating employee {EmployeeName}", request.EmployeeName);
                return StatusCode(500, "Internal server error while creating employee.");
            }
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteEmployeeAsync(int id)
        {
            try
            {
                _logger.LogInformation("[DeleteEmployeeAsync]: Deleting employee with ID {EmployeeId}", id);
                // Assuming a method DeleteEmployeeAsync exists in the service
                await _employeeService.DeleteEmployeeAsync(id);
                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "DeleteEmployee",
                    Details = $"Deleted employee with ID {id}",
                    DurationMs = 0,
                    EntityId = id,
                    EntityType = "Employee",
                    ErrorMessage = null,
                    HttpMethod = "POST",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IsSuccess = true,
                    RequestPath = HttpContext.Request.Path,
                    StatusCode = 200,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser
                });
                return Ok(new { Message = "Employee deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeleteEmployeeAsync]: Error occurred while deleting employee with ID {EmployeeId}", id);
                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "DeleteEmployee",
                    Details = $"Failed to delete employee with ID {id}",
                    DurationMs = 0,
                    EntityId = id,
                    EntityType = "Employee",
                    ErrorMessage = ex.Message,
                    HttpMethod = "POST",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    IsSuccess = false,
                    RequestPath = HttpContext.Request.Path,
                    StatusCode = 500,
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser
                });
                return StatusCode(500, "Internal server error while deleting employee.");
            }
        }
    }
}