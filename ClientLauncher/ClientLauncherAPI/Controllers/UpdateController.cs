using ClientLauncher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly ILogger<UpdateController> _logger;
        private readonly IUpdateService _updateService;
        private readonly IConfiguration _configuration;

        public UpdateController(
            ILogger<UpdateController> logger,
            IUpdateService updateService,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _updateService = updateService;
            _configuration = configuration;
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckForUpdates([FromQuery] string appCode)
        {
            try
            {
                var updateApplied = await _updateService.CheckAndApplyUpdatesAsync(appCode);
                if (updateApplied)
                {
                    return Ok("Update applied");
                }
                else
                {
                    return Ok("No updates available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates for {AppCode}", appCode);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Check for ClientLauncher self-update
        /// </summary>
        [HttpGet("clientlauncher/check")]
        public IActionResult CheckForClientLauncherUpdates()
        {
            try
            {
                // TODO: Get from database or configuration
                var latestVersion = new
                {
                    Version = "2.0.0.0",
                    DownloadUrl = $"{Request.Scheme}://{Request.Host}/Packages/ClientApplication/1.1.2/ClientApplication_1.1.2.zip",
                    ReleaseNotes = "- Added Windows Service support\n- Added auto-update functionality\n- Bug fixes and improvements",
                    ReleasedAt = DateTime.UtcNow,
                    FileSizeBytes = 10485760L, // 10MB example
                    IsCritical = false
                };

                return Ok(latestVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for ClientLauncher updates");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Download latest ClientLauncher package
        /// </summary>
        [HttpGet("clientlauncher/download")]
        public IActionResult DownloadClientLauncherUpdate()
        {
            try
            {
                var updatePackagePath = Path.Combine(
                    _configuration["UpdatePackagesPath"] ?? @"C:\Updates",
                    "ClientLauncher_Latest.zip");

                if (!System.IO.File.Exists(updatePackagePath))
                {
                    return NotFound(new { success = false, message = "Update package not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(updatePackagePath);
                return File(fileBytes, "application/zip", "ClientLauncher_Update.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading ClientLauncher update package");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload new ClientLauncher version (Admin only)
        /// </summary>
        [HttpPost("clientlauncher/upload")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadClientLauncherUpdate([FromForm] IFormFile file, [FromForm] string version, [FromForm] string releaseNotes)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded" });
                }

                var updatePackagesPath = _configuration["UpdatePackagesPath"] ?? @"C:\Updates";
                Directory.CreateDirectory(updatePackagesPath);

                var fileName = $"ClientLauncher_{version}.zip";
                var filePath = Path.Combine(updatePackagesPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Copy to "Latest" as well
                var latestPath = Path.Combine(updatePackagesPath, "ClientLauncher_Latest.zip");
                System.IO.File.Copy(filePath, latestPath, true);

                _logger.LogInformation("ClientLauncher update package uploaded: {Version}", version);

                return Ok(new
                {
                    success = true,
                    message = "Update package uploaded successfully",
                    data = new
                    {
                        version,
                        fileName,
                        fileSize = file.Length,
                        releaseNotes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading ClientLauncher update package");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
