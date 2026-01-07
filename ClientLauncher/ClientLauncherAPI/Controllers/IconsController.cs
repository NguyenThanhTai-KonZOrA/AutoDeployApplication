using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.ViewModels.Request;
using Microsoft.AspNetCore.Mvc;

namespace ClientLauncherAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IconsController : ControllerBase
    {
        private readonly IIconsService _iconsService;

        public IconsController(IIconsService iconsService)
        {
            _iconsService = iconsService;
        }

        /// <summary>
        /// Get all icons
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var icons = await _iconsService.GetAllAsync();
                return Ok(icons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icon by ID
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var icon = await _iconsService.GetByIdAsync(id);
                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                return Ok(icon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icons by type
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetByType(IconType type)
        {
            try
            {
                var icons = await _iconsService.GetByTypeAsync(type);
                return Ok(icons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get icon by type and reference ID
        /// </summary>
        [HttpGet("type/{type}/reference/{referenceId}")]
        public async Task<IActionResult> GetByTypeAndReferenceId(IconType type, int referenceId)
        {
            try
            {
                var icon = await _iconsService.GetByTypeAndReferenceIdAsync(type, referenceId);
                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                return Ok(icon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new icon with file upload
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateIconRequest createRequest)
        {
            try
            {
                var createdBy = User?.Identity?.Name ?? "System";
                var icon = await _iconsService.CreateAsync(createRequest, createdBy);
                return CreatedAtAction(nameof(GetById), new { id = icon.Id }, icon);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Update icon information
        /// </summary>
        [HttpPost("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateIconRequest updateDto)
        {
            try
            {
                var updatedBy = User?.Identity?.Name ?? "System";
                var icon = await _iconsService.UpdateAsync(id, updateDto, updatedBy);

                if (icon == null)
                    return NotFound(new { message = "Icon not found" });

                return Ok(icon);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Upload/Replace icon file
        /// </summary>
        //[HttpPost("upload/{id}")]
        //public async Task<IActionResult> UploadIcon(int id, [FromForm] IFormFile file)
        //{
        //    try
        //    {
        //        var updatedBy = User?.Identity?.Name ?? "System";
        //        var icon = await _iconsService.UploadIconAsync(id, file, updatedBy);

        //        if (icon == null)
        //            return NotFound(new { message = "Icon not found" });

        //        return Ok(icon);
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //    }
        //}

        /// <summary>
        /// Delete icon (soft delete)
        /// </summary>
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deletedBy = User?.Identity?.Name ?? "System";
                var result = await _iconsService.DeleteAsync(id, deletedBy);

                if (!result)
                    return NotFound(new { message = "Icon not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}