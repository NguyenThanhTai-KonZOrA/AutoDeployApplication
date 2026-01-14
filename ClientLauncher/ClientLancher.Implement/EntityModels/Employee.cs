using ClientLauncher.Common.BaseEntity;
using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.EntityModels
{
    public class Employee : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EmployeeCode { get; set; } = string.Empty;
        public string WindowAccount { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        // Navigation properties
        public virtual ICollection<EmployeeRole>? EmployeeRoles { get; set; }
    }
}
