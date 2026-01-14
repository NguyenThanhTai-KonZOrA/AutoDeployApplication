using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IPermissionService
    {
        Task<List<PermissionResponse>> GetAllPermissionsAsync();
        Task<PermissionResponse?> GetPermissionByIdAsync(int permissionId);
        Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request, string createdBy);
        Task<PermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request, string updatedBy);
        Task<bool> DeletePermissionAsync(int permissionId, string deletedBy);
        Task<bool> ToggleActiveAsync(int permissionId, string updatedBy);
        Task<Dictionary<string, List<PermissionResponse>>> GetPermissionsByCategoryAsync();
    }
}