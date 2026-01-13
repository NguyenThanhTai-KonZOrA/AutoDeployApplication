using ClientLauncher.Common.BaseEntity;
using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.EntityModels
{
    public class Permission : BaseEntity
    {
        [Key]
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

        // Navigation properties
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
    }
}