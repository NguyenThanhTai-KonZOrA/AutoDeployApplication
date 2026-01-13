using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IRoleService
    {
        Task<List<RoleResponse>> GetAllRolesAsync();
        Task<RoleResponse?> GetRoleByIdAsync(int roleId);
        Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, string createdBy);
        Task<RoleResponse> UpdateRoleAsync(UpdateRoleRequest request, string updatedBy);
        Task<bool> DeleteRoleAsync(int roleId, string deletedBy);
        Task<bool> ToggleActiveAsync(int roleId, string updatedBy);
        Task<List<RoleResponse>> GetActiveRolesByIdsAsync(List<int> roleIds);
    }
}