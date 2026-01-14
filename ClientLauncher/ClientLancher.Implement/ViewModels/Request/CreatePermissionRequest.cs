using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.ViewModels.Request
{
    public class CreatePermissionRequest
    {
        [Required]
        [StringLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PermissionCode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }
    }
}