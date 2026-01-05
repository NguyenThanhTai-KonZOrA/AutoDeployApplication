using ClientLancher.Implement.ViewModels.Request;
using ClientLancher.Implement.ViewModels.Response;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IApplicationManagementService
    {
        // Application CRUD
        Task<ApplicationDetailResponse> CreateApplicationAsync(ApplicationCreateRequest request);
        Task<ApplicationDetailResponse> UpdateApplicationAsync(int id, ApplicationUpdateRequest request);
        Task<bool> DeleteApplicationAsync(int id);
        Task<ApplicationDetailResponse?> GetApplicationByIdAsync(int id);
        Task<ApplicationDetailResponse?> GetApplicationByCodeAsync(string appCode);
        Task<IEnumerable<ApplicationDetailResponse>> GetAllApplicationsAsync();
        Task<IEnumerable<ApplicationDetailResponse>> GetApplicationsByCategoryAsync(int categoryId);
        Task<bool> ChangeApplicationStatusAsync(int applicationId);
        // Statistics
        Task<ApplicationDetailResponse> GetApplicationWithStatsAsync(int id);
    }
}