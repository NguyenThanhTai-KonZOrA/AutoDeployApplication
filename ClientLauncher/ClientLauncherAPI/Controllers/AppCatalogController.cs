using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppCatalogController : ControllerBase
    {
        private readonly IAppCatalogService _appCatalogService;
        private readonly ILogger<AppCatalogController> _logger;
        private readonly IEmployeeService _employeeService;
        private readonly IAuditLogService _auditLogService;

        public AppCatalogController(
            IAppCatalogService appCatalogService,
            ILogger<AppCatalogController> logger,
            IEmployeeService employeeService,
            IAuditLogService auditLogService)
        {
            _appCatalogService = appCatalogService;
            _logger = logger;
            _employeeService = employeeService;
            _auditLogService = auditLogService;
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetAllApplications([FromQuery] string userName)
        {
            try
            {
                var isAdmin = await _employeeService.IsUserAdminAsync(userName);
                if (!isAdmin)
                {
                    _logger.LogWarning("User {UserName} attempted to access all applications without permission", userName);
                    // Audit log entry
                    await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                    {
                        UserName = userName,
                        Action = "Access All Applications",
                        IsSuccess = false,
                        ErrorMessage = "User does not have permission to access all applications",
                        Details = $"User {userName} attempted to access all applications without admin rights.",
                        DurationMs = 0,
                        EntityId = null,
                        EntityType = "Application",
                        HttpMethod = "GET",
                        RequestPath = HttpContext.Request.Path,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                        StatusCode = 403
                    });

                    return Forbid("User does not have permission to access all applications");
                }

                var apps = await _appCatalogService.GetAllApplicationsAsync();
                if (apps == null || !apps.Any())
                {
                    return Ok(new List<ApplicationResponse>());
                }

                var mappings = apps.Select(app => new ApplicationResponse
                {
                    AppCode = app.AppCode,
                    Name = app.Name,
                    Description = app.Description,
                    Category = app.Category?.Name ?? string.Empty,
                    IsActive = app.IsActive,
                    CreatedAt = app.CreatedAt,
                    UpdatedAt = app.UpdatedAt,
                    IconUrl = app.IconUrl,
                    Id = app.Id,
                });

                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching applications");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("applications/category/{category}")]
        public async Task<IActionResult> GetApplicationsByCategory(string category)
        {
            try
            {
                var apps = await _appCatalogService.GetApplicationsByCategoryAsync(category);
                return Ok(apps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching applications for category {Category}", category);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("applications/{appCode}")]
        public async Task<IActionResult> GetApplication(string appCode)
        {
            try
            {
                var app = await _appCatalogService.GetApplicationAsync(appCode);
                if (app == null)
                {
                    return NotFound($"Application {appCode} not found");
                }
                return Ok(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching application {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("applications/{appCode}/installed")]
        public async Task<IActionResult> IsApplicationInstalled(string appCode)
        {
            try
            {
                var isInstalled = await _appCatalogService.IsApplicationInstalledAsync(appCode);
                return Ok(new { appCode, isInstalled });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking installation status for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("applications/{appCode}/version")]
        public async Task<IActionResult> GetInstalledVersion(string appCode)
        {
            try
            {
                var version = await _appCatalogService.GetInstalledVersionAsync(appCode);
                if (version == null)
                {
                    return NotFound($"Application {appCode} is not installed");
                }
                return Ok(new { appCode, version });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting installed version for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}