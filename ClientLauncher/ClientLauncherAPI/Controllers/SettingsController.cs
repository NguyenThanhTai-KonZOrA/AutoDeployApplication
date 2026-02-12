using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientLauncherAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly IApplicationSettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;
        private readonly IAuditLogService _auditLogService;
        private static readonly HashSet<string> RestartRequiredSettings = new()
        {
            "Jwt:Key",
            "Jwt:Issuer",
            "Jwt:Audience",
            "Jwt:ExpireMinutes"
        };

        public SettingsController(
            IApplicationSettingsService settingsService,
            ILogger<SettingsController> logger,
            IAuditLogService auditLogService)
        {
            _settingsService = settingsService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                _logger.LogInformation("[GetAllSettings]: Start Getting all settings");
                var settings = await _settingsService.GetAllSettingsAsync();
                _logger.LogInformation("[GetAllSettings]: Finished Getting all settings");
                return Ok(settings);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "[GetAllSettings]: Error getting all settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetSettingByKey(string key)
        {
            try
            {
                _logger.LogInformation("[GetSettingByKey]: Start Getting setting by key: {Key}", key);
                var setting = await _settingsService.GetSettingByKeyAsync(key);
                if (setting == null)
                    return NotFound($"Setting with key '{key}' not found");

                _logger.LogInformation("[GetSettingByKey]: Finished Getting setting by key: {Key}", key);
                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetSettingByKey]: Error getting setting by key: {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetSettingsByCategory(string category)
        {
            try
            {
                var settings = await _settingsService.GetSettingsByCategoryAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings by category: {Category}", category);
                return StatusCode(500, "Internal server error");
            }
        }

        // Add new setting
        [HttpPost("create")]
        public async Task<IActionResult> AddSetting([FromBody] CreateSettingRequest request)
        {
            try
            {
                _logger.LogInformation("[AddSetting]: Adding new setting: {Key}", request.Key);
                // Validate request
                if (string.IsNullOrWhiteSpace(request.Key))
                {
                    return BadRequest(new { success = false, message = "Setting key is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Value))
                {
                    return BadRequest(new { success = false, message = "Setting value is required" });
                }
                _logger.LogInformation("[AddSetting]: Validated setting: {Key}", request.Key);

                // Check if key already exists
                var existing = await _settingsService.GetSettingByKeyAsync(request.Key);
                if (existing != null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Setting with key '{request.Key}' already exists",
                        existingSetting = existing
                    });
                }
                _logger.LogInformation("[AddSetting]: Setting key is unique: {Key}", request.Key);
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                var result = await _settingsService.AddSettingAsync(request, userName);

                if (!result)
                {
                    return StatusCode(500, new { success = false, message = "Failed to add setting" });
                }

                _logger.LogInformation("[AddSetting]: Setting {Key} created by {User}", request.Key, userName);

                // Check if restart required
                var requiresRestart = RestartRequiredSettings.Contains(request.Key);
                _logger.LogInformation("[AddSetting]: Setting {Key} requires restart: {RequiresRestart}", request.Key, requiresRestart);

                // Audit log
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    Action = "CreateSetting",
                    EntityType = "Setting",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    UserName = userName,
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status201Created,
                    Details = $"Created setting '{request.Key}' with value '{request.Value}'",
                    DurationMs = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    ErrorMessage = null
                });

                return CreatedAtAction(
                    nameof(GetSettingByKey),
                    new { key = request.Key },
                    new
                    {
                        success = true,
                        message = "Setting created successfully",
                        key = request.Key,
                        value = request.Value,
                        category = request.Category,
                        requiresRestart = requiresRestart,
                        warning = requiresRestart
                            ? "⚠️ This setting requires application restart to take effect. Please contact system administrator."
                            : null
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddSetting]: Error adding setting: {Key}", request.Key);
                // Audit log for failure
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    Action = "CreateSetting",
                    EntityType = "Setting",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    UserName = userName,
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Details = $"Failed to create setting '{request.Key}'",
                    DurationMs = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    ErrorMessage = ex.Message
                });
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                var result = await _settingsService.UpdateSettingAsync(key, request.Value, userName);

                if (!result)
                    return NotFound($"Setting with key '{key}' not found");

                _logger.LogInformation("Setting {Key} updated to {Value} by {User}", key, request.Value, userName);

                // ✅ Check if restart required
                var requiresRestart = RestartRequiredSettings.Contains(key);

                // Audit log
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    Action = "UpdateSetting",
                    EntityType = "Setting",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    UserName = userName,
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Details = $"Updated setting '{key}' to '{request.Value}'. Requires restart.",
                    DurationMs = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    ErrorMessage = null
                });

                return Ok(new
                {
                    success = true,
                    message = "Setting updated successfully",
                    key = key,
                    value = request.Value,
                    requiresRestart = requiresRestart,
                    warning = requiresRestart
                        ? "⚠️ This setting requires application restart to take effect. Please contact system administrator."
                        : null,
                    appliedAt = requiresRestart ? "After restart" : "Within 5 minutes"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting: {Key}", key);
                // Audit log for failure
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                await _auditLogService.LogActionAsync(new ClientLauncher.Implement.ViewModels.Request.CreateAuditLogRequest
                {
                    Action = "UpdateSetting",
                    EntityType = "Setting",
                    EntityId = null,
                    HttpMethod = "POST",
                    RequestPath = HttpContext.Request.Path,
                    UserName = userName,
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Details = $"Failed to update setting '{key}'",
                    DurationMs = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    ErrorMessage = ex.Message
                });
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkUpdateSettings([FromBody] BulkUpdateSettingsRequest request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                await _settingsService.BulkUpdateSettingsAsync(request, userName);

                _logger.LogInformation("Bulk settings updated by {User}: {Count} settings", userName, request.Settings.Count);

                // ✅ Check which settings require restart
                var restartRequired = request.Settings.Where(s => RestartRequiredSettings.Contains(s.Key)).Select(s => s.Key).ToList();

                var immediateApply = request.Settings.Where(s => !RestartRequiredSettings.Contains(s.Key)).Select(s => s.Key).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Settings updated successfully",
                    totalUpdated = request.Settings.Count,
                    immediateApply = new
                    {
                        count = immediateApply.Count,
                        keys = immediateApply,
                        appliedAt = "Within 5 minutes"
                    },
                    restartRequired = new
                    {
                        count = restartRequired.Count,
                        keys = restartRequired,
                        warning = restartRequired.Any()
                            ? "⚠️ The following settings require application restart: " + string.Join(", ", restartRequired)
                            : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating settings");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("delete/{key}")]
        public async Task<IActionResult> DeleteSetting(string key)
        {
            try
            {
                _logger.LogInformation("[DeleteSetting]: Attempting to delete setting: {Key}", key);
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                var result = await _settingsService.DeleteSettingAsync(key, userName);

                if (!result)
                    return NotFound($"Setting with key '{key}' not found");

                _logger.LogInformation("[DeleteSetting]: Setting {Key} deleted by {User}", key, userName);

                var requiresRestart = RestartRequiredSettings.Contains(key);

                return Ok(new
                {
                    success = true,
                    message = "Setting deleted successfully",
                    key = key,
                    requiresRestart = requiresRestart,
                    warning = requiresRestart
                        ? "⚠️ This setting requires application restart to take effect."
                        : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeleteSetting]: Error deleting setting: {Key}", key);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // Force clear cache for specific setting
        [HttpPost("clear-cache/{key}")]
        public IActionResult ClearCache(string key)
        {
            try
            {
                _logger.LogInformation("[ClearCache]: Clearing cache for setting: {Key}", key);
                _settingsService.ClearCache(key);
                _logger.LogInformation("[ClearCache]: Cache cleared for setting: {Key}", key);

                return Ok(new
                {
                    success = true,
                    message = $"Cache cleared for '{key}'. New value will be loaded on next access.",
                    key = key
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for key: {Key}", key);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // Get settings info (which require restart)
        [HttpGet("info")]
        public IActionResult GetSettingsInfo()
        {
            _logger.LogInformation("[GetSettingsInfo]: Retrieving settings metadata");
            return Ok(new
            {
                cacheExpirationMinutes = _settingsService.GetSettingValue<int>(CommonConstants.CacheExpirationMinutesKey),
                restartRequiredSettings = RestartRequiredSettings,
                categories = new[]
                {
                    new { name = "System", appliesImmediately = true, description = "General system settings" }
                },
                dataTypes = new[] { "String", "Integer", "Double", "Boolean", "Json" }
            });
        }
    }
}