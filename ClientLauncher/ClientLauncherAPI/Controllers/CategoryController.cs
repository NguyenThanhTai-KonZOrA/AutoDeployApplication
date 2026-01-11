using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;
        private readonly IAuditLogService _auditLogService;

        public CategoryController(
            ICategoryService categoryService,
            ILogger<CategoryController> logger,
            IAuditLogService auditLogService)
        {
            _categoryService = categoryService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new category: {CategoryName}", request.Name);
                var result = await _categoryService.CreateCategoryAsync(request);
                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "CreateCategory",
                    EntityType = "Category",
                    EntityId = result.Id,
                    IsSuccess = true,
                    Details = $"Category '{request.Name}' created successfully.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = null,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 200
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "CreateCategory",
                    EntityType = "Category",
                    EntityId = null,
                    IsSuccess = false,
                    Details = $"Failed to create category '{request.Name}'.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = ex.Message,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 500
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryCreateRequest request)
        {
            try
            {
                _logger.LogInformation("Updating category ID: {Id}", id);
                var result = await _categoryService.UpdateCategoryAsync(id, request);

                //audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "UpdateCategory",
                    EntityType = "Category",
                    EntityId = id,
                    IsSuccess = true,
                    Details = $"Category ID '{id}' updated successfully.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = null,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 200
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category ID: {Id}", id);
                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "UpdateCategory",
                    EntityType = "Category",
                    EntityId = id,
                    IsSuccess = false,
                    Details = $"Failed to update category ID '{id}'.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = ex.Message,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 500
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a category
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                _logger.LogInformation("Deleting category ID: {Id}", id);
                var result = await _categoryService.DeleteCategoryAsync(id);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }

                // Audit log entry
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "DeleteCategory",
                    EntityType = "Category",
                    EntityId = id,
                    IsSuccess = true,
                    Details = $"Category ID '{id}' deleted successfully.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = null,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 200
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category ID: {Id}", id);
                // Audit log entry for failure
                await _auditLogService.LogActionAsync(new CreateAuditLogRequest
                {
                    Action = "DeleteCategory",
                    EntityType = "Category",
                    EntityId = id,
                    IsSuccess = false,
                    Details = $"Failed to delete category ID '{id}'.",
                    DurationMs = 0,
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ErrorMessage = ex.Message,
                    HttpMethod = Request.Method,
                    RequestPath = Request.Path,
                    UserName = User.Identity?.Name ?? "Anonymous",
                    StatusCode = 500
                });
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var result = await _categoryService.GetCategoryByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get category by name
        /// </summary>
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetCategoryByName(string name)
        {
            try
            {
                var result = await _categoryService.GetCategoryByNameAsync(name);
                if (result == null)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by name: {Name}", name);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var result = await _categoryService.GetAllCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get active categories only
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCategories()
        {
            try
            {
                var result = await _categoryService.GetActiveCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active categories");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}