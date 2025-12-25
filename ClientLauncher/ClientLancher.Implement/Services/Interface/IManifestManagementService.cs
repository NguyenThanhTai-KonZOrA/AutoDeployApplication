using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IManifestManagementService
    {
        // CRUD Operations
        Task<ManifestResponse> CreateManifestAsync(int applicationId, ManifestCreateRequest request, string? createdBy = null);
        Task<ManifestResponse> UpdateManifestAsync(int manifestId, ManifestUpdateRequest request, string? updatedBy = null);
        Task<bool> DeleteManifestAsync(int manifestId);

        // Query Operations
        Task<ManifestResponse?> GetManifestByIdAsync(int manifestId);
        Task<ManifestResponse?> GetLatestManifestAsync(int applicationId);
        Task<ManifestResponse?> GetLatestManifestByAppCodeAsync(string appCode);
        Task<ManifestResponse?> GetManifestByVersionAsync(int applicationId, string version);
        Task<IEnumerable<ManifestResponse>> GetManifestHistoryAsync(int applicationId, int take = 10);

        // Generate manifest JSON
        Task<ManifestJsonResponse?> GenerateManifestJsonAsync(string appCode);

        // Activation
        Task<bool> ActivateManifestAsync(int manifestId);
        Task<bool> DeactivateManifestAsync(int manifestId);
    }
}