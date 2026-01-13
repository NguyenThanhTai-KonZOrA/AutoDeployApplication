using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IRolePermissionRepository : IGenericRepository<RolePermission>
    {
        Task<List<RolePermission>> GetByRoleIdAsync(int roleId);
        Task RemoveByRoleIdAsync(int roleId);
    }
}