using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IDeploymentHistoryRepository : IGenericRepository<DeploymentHistory>
    {
        Task<IEnumerable<DeploymentHistory>> GetByPackageVersionIdAsync(int packageVersionId);
        Task<IEnumerable<DeploymentHistory>> GetByEnvironmentAsync(string environment);
        Task<IEnumerable<DeploymentHistory>> GetPendingDeploymentsAsync();
        Task<IEnumerable<DeploymentHistory>> GetRecentDeploymentsAsync(int take = 20);
        Task<DeploymentHistory?> GetLatestDeploymentAsync(int packageVersionId);
        Task<int> GetSuccessRateAsync(int packageVersionId);
    }
}