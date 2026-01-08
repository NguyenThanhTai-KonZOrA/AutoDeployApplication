using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackageManagementController : ControllerBase
    {
        private readonly IPackageVersionService _packageService;
        private readonly ILogger<PackageManagementController> _logger;

        public PackageManagementController(
            IPackageVersionService packageService,
            ILogger<PackageManagementController> logger)
        {
            _packageService = packageService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a new package version
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(524288000)] // 500MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<IActionResult> UploadPackage([FromForm] PackageUploadRequest request)
        {
            try
            {
                _logger.LogInformation("Package upload request received for Application ID: {ApplicationId}",
                    request.ApplicationId);

                var result = await _packageService.UploadPackageAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading package");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update package metadata
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> UpdatePackage([FromForm] PackageUpdateRequest request)
        {
            try
            {
                var result = await _packageService.UpdatePackageAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating package ID: {Id}", request.Id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a package version
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeletePackage(int id)
        {
            try
            {
                var result = await _packageService.DeletePackageAsync(id);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Package not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting package ID: {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Publish a package version
        /// </summary>
        [HttpPost("publish")]
        public async Task<IActionResult> PublishPackage([FromBody] PublishPackageRequest request)
        {
            try
            {
                var result = await _packageService.PublishPackageAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing package");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get package by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackage(int id)
        {
            try
            {
                var result = await _packageService.GetPackageByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Package not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all versions for an application
        /// </summary>
        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetPackagesByApplication(int applicationId)
        {
            try
            {
                var result = await _packageService.GetPackagesByApplicationIdAsync(applicationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting packages for application ID: {ApplicationId}", applicationId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all packages grouped by application
        /// </summary>
        [HttpGet("all-by-applications")]
        public async Task<IActionResult> GetAllPackagesByApplications()
        {
            try
            {
                _logger.LogInformation("Fetching all packages grouped by application");

                var result = await _packageService.GetAllPackagesGroupedByApplicationAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all packages grouped by application");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get latest version for an application
        /// </summary>
        [HttpGet("application/{applicationId}/latest")]
        public async Task<IActionResult> GetLatestVersion(int applicationId, [FromQuery] bool stableOnly = true)
        {
            try
            {
                var result = await _packageService.GetLatestVersionAsync(applicationId, stableOnly);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "No versions found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest version for application ID: {ApplicationId}", applicationId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get version history for an application
        /// </summary>
        [HttpGet("application/{applicationId}/history")]
        public async Task<IActionResult> GetVersionHistory(int applicationId, [FromQuery] int take = 10)
        {
            try
            {
                var result = await _packageService.GetVersionHistoryAsync(applicationId, take);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting version history for application ID: {ApplicationId}", applicationId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Download a package file
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadPackage(int id)
        {
            try
            {
                var (fileData, fileName, contentType) = await _packageService.DownloadPackageAsync(id);

                // Record download statistic
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var machineName = Request.Headers["X-Machine-Name"].ToString() ?? "Unknown";
                var userName = Request.Headers["X-User-Name"].ToString() ?? "Unknown";

                await _packageService.RecordDownloadStatisticAsync(
                    id, machineName, userName, ipAddress, true, fileData.Length, 0);

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading package ID: {Id}", id);
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Rollback to a previous version
        /// </summary>
        [HttpPost("application/{applicationId}/rollback")]
        public async Task<IActionResult> RollbackVersion(
            int applicationId,
            [FromQuery] string version,
            [FromQuery] string performedBy)
        {
            try
            {
                var result = await _packageService.RollbackToVersionAsync(applicationId, version, performedBy);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rollback");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}