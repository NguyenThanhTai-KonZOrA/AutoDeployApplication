using ClientLancher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppCatalogController : ControllerBase
    {
        private readonly IAppCatalogService _appCatalogService;
        private readonly ILogger<AppCatalogController> _logger;

        public AppCatalogController(
            IAppCatalogService appCatalogService,
            ILogger<AppCatalogController> logger)
        {
            _appCatalogService = appCatalogService;
            _logger = logger;
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var apps = await _appCatalogService.GetAllApplicationsAsync();
                return Ok(apps);
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