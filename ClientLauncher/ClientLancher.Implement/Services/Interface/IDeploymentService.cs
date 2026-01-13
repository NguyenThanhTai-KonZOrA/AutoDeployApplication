using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IDeploymentService
    {
        // Deployment CRUD
        Task<DeploymentResponse> CreateDeploymentAsync(DeploymentCreateRequest request);
        Task<DeploymentResponse> UpdateDeploymentStatusAsync(int id, string status, string? errorMessage = null);
        Task<bool> CancelDeploymentAsync(int id, string cancelledBy);
        Task<DeploymentResponse?> GetDeploymentByIdAsync(int id);
        Task<IEnumerable<DeploymentResponse>> GetAllDeploymentsAsync();
        Task<IEnumerable<DeploymentResponse>> GetDeploymentsByEnvironmentAsync(string environment);
        Task<IEnumerable<DeploymentResponse>> GetPendingDeploymentsAsync();

        // Approval Management
        Task<DeploymentResponse> ApproveDeploymentAsync(int id, string approvedBy);
        Task<DeploymentResponse> RejectDeploymentAsync(int id, string rejectedBy, string reason);

        // Progress Tracking
        Task UpdateDeploymentProgressAsync(int id, bool success, string? errorMessage = null);
        Task<DeploymentProgressResponse> GetDeploymentProgressAsync(int id);

        // Deployment Execution
        Task<DeploymentResponse> StartDeploymentAsync(int id);
        Task CompleteDeploymentAsync(int id);

        // Filtering & Search
        Task<IEnumerable<DeploymentResponse>> GetDeploymentsByPackageVersionAsync(int packageVersionId);
        Task<IEnumerable<DeploymentResponse>> GetDeploymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<DeploymentResponse?> GetLatestDeploymentForApplicationAsync(int applicationId);
    }
}