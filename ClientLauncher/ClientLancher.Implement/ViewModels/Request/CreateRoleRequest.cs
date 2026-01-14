using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.ViewModels.Request
{
    public class CreateRoleRequest
    {
        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public List<int>? PermissionIds { get; set; }
    }
}