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

        public ApplicationManagementController(
            IApplicationManagementService appService,
            ILogger<ApplicationManagementController> logger)
        {
            _appService = appService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new application
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] ApplicationCreateRequest request)
        {
            try
            {
                var result = await _appService.CreateApplicationAsync(request);
                return CreatedAtAction(nameof(GetApplication), new { id = result.Id },
                    new { success = true, data = result, message = "Application created successfully" });
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
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApplication(int id, [FromBody] ApplicationUpdateRequest request)
        {
            try
            {
                var result = await _appService.UpdateApplicationAsync(id, request);
                return Ok(new { success = true, data = result, message = "Application updated successfully" });
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                var result = await _appService.DeleteApplicationAsync(id);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Application not found" });
                }
                return Ok(new { success = true, message = "Application deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application ID: {Id}", id);
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
                return Ok(new { success = true, data = result });
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
        [HttpGet]
        public async Task<IActionResult> GetAllApplications()
        {
            try
            {
                var result = await _appService.GetAllApplicationsAsync();
                return Ok(new { success = true, data = result });
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
                return Ok(new { success = true, data = result });
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
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application stats for ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}