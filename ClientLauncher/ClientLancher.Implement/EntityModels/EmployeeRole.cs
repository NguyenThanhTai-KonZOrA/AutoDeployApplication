using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class EmployeeRole : BaseEntity
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }

        // Navigation properties
        public virtual Employee? Employee { get; set; }
        public virtual Role? Role { get; set; }
    }
}