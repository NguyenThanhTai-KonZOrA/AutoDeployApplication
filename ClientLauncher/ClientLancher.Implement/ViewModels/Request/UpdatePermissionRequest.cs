using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.ViewModels.Request
{
    public class UpdatePermissionRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PermissionName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PermissionCode { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }
    }
}