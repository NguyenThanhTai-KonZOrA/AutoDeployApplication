using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.EntityModels;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLancher.Implement.Repositories
{
    public class DeploymentHistoryRepository : GenericRepository<DeploymentHistory>, IDeploymentHistoryRepository
    {
        public DeploymentHistoryRepository(ClientLancherDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DeploymentHistory>> GetByPackageVersionIdAsync(int packageVersionId)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Where(d => d.PackageVersionId == packageVersionId)
                .OrderByDescending(d => d.DeployedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentHistory>> GetByEnvironmentAsync(string environment)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Where(d => d.Environment == environment)
                .OrderByDescending(d => d.DeployedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentHistory>> GetPendingDeploymentsAsync()
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .Where(d => d.Status == "Pending" || d.Status == "InProgress")
                .OrderBy(d => d.DeployedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DeploymentHistory>> GetRecentDeploymentsAsync(int take = 20)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                    .ThenInclude(p => p.Application)
                .OrderByDescending(d => d.DeployedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<DeploymentHistory?> GetLatestDeploymentAsync(int packageVersionId)
        {
            return await _dbSet
                .Include(d => d.PackageVersion)
                .Where(d => d.PackageVersionId == packageVersionId)
                .OrderByDescending(d => d.DeployedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetSuccessRateAsync(int packageVersionId)
        {
            var deployments = await _dbSet
                .Where(d => d.PackageVersionId == packageVersionId && d.CompletedAt != null)
                .ToListAsync();

            if (!deployments.Any())
                return 0;

            var successCount = deployments.Count(d => d.Status == "Success");
            return (int)((double)successCount / deployments.Count * 100);
        }
    }
}