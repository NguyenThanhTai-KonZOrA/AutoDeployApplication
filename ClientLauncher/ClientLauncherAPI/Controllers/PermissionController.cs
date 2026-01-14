using ClientLauncher.Common.Constants;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(IPermissionService permissionService, ILogger<PermissionController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                _logger.LogInformation("[GetAllPermissions]: Retrieving all permissions");
                var result = await _permissionService.GetAllPermissionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetAllPermissions]: Error retrieving permissions");
                return StatusCode(500, new { message = "An error occurred while retrieving permissions", error = ex.Message });
            }
        }

        [HttpGet("by-category")]
        public async Task<IActionResult> GetPermissionsByCategory()
        {
            try
            {
                _logger.LogInformation("[GetPermissionsByCategory]: Retrieving permissions grouped by category");
                var result = await _permissionService.GetPermissionsByCategoryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPermissionsByCategory]: Error retrieving permissions by category");
                return StatusCode(500, new { message = "An error occurred while retrieving permissions", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermissionById(int id)
        {
            try
            {
                _logger.LogInformation("[GetPermissionById]: Retrieving permission {Id}", id);
                var result = await _permissionService.GetPermissionByIdAsync(id);

                if (result == null)
                {
                    _logger.LogWarning("[GetPermissionById]: Permission {Id} not found", id);
                    return NotFound(new { message = $"Permission with ID {id} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetPermissionById]: Error retrieving permission {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving permission", error = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                _logger.LogInformation("[CreatePermission]: Creating permission {Code} by {User}", request.PermissionCode, userName);

                var result = await _permissionService.CreatePermissionAsync(request, userName);
                _logger.LogInformation("[CreatePermission]: Permission created successfully with ID {Id}", result.Id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "[CreatePermission]: Validation error creating permission");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreatePermission]: Error creating permission");
                return StatusCode(500, new { message = "An error occurred while creating permission", error = ex.Message });
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdatePermission(int id, [FromBody] UpdatePermissionRequest request)
        {
            try
            {
                if (id != request.Id)
                {
                    return BadRequest(new { message = "ID in URL does not match ID in request body" });
                }

                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                _logger.LogInformation("[UpdatePermission]: Updating permission {Id} by {User}", id, userName);

                var result = await _permissionService.UpdatePermissionAsync(request, userName);
                _logger.LogInformation("[UpdatePermission]: Permission {Id} updated successfully", id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "[UpdatePermission]: Validation error updating permission {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdatePermission]: Error updating permission {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating permission", error = ex.Message });
            }
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                _logger.LogInformation("[DeletePermission]: Deleting permission {Id} by {User}", id, userName);

                var result = await _permissionService.DeletePermissionAsync(id, userName);
                _logger.LogInformation("[DeletePermission]: Permission {Id} deleted successfully", id);
                return Ok(new { message = "Permission deleted successfully", success = result });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "[DeletePermission]: Validation error deleting permission {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeletePermission]: Error deleting permission {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting permission", error = ex.Message });
            }
        }

        [HttpPost("change-status/{id}")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? CommonConstants.UnknownUser;
                _logger.LogInformation("[ToggleActive]: Toggling active status for permission {Id} by {User}", id, userName);

                var isActive = await _permissionService.ToggleActiveAsync(id, userName);
                _logger.LogInformation("[ToggleActive]: Permission {Id} active status toggled to {IsActive}", id, isActive);
                return Ok(new { message = $"Permission {(isActive ? "activated" : "deactivated")} successfully", isActive });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "[ToggleActive]: Validation error toggling permission {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ToggleActive]: Error toggling permission {Id}", id);
                return StatusCode(500, new { message = "An error occurred while toggling permission status", error = ex.Message });
            }
        }
    }
}