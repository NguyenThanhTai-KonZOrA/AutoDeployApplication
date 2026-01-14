using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class RolePermission : BaseEntity
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        // Navigation properties
        public virtual Role? Role { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}