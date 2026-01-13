using ClientLauncher.Common.BaseEntity;
using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.EntityModels
{
    public class Role : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<EmployeeRole>? EmployeeRoles { get; set; }
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
    }
}