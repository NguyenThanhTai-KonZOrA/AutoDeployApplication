using ClientLancher.Implement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard statistics overview
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                var result = await _analyticsService.GetDashboardStatisticsAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recent downloads
        /// </summary>
        [HttpGet("downloads/recent")]
        public async Task<IActionResult> GetRecentDownloads([FromQuery] int take = 50)
        {
            try
            {
                var result = await _analyticsService.GetRecentDownloadsAsync(take);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent downloads");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get downloads by date for a specific package version
        /// </summary>
        [HttpGet("downloads/by-date/{packageVersionId}")]
        public async Task<IActionResult> GetDownloadsByDate(int packageVersionId, [FromQuery] int days = 30)
        {
            try
            {
                var result = await _analyticsService.GetDownloadsByDateAsync(packageVersionId, days);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting downloads by date for package {PackageVersionId}", packageVersionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get top applications by download count
        /// </summary>
        [HttpGet("applications/top")]
        public async Task<IActionResult> GetTopApplications([FromQuery] int take = 10)
        {
            try
            {
                var result = await _analyticsService.GetTopApplicationsAsync(take);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top applications");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}