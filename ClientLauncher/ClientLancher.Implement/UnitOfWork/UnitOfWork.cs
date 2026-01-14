using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.Repositories;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClientLauncher.Implement.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DeploymentManagerDbContext _context;
        private IDbContextTransaction? _transaction;

        public IApplicationRepository Applications { get; }
        public IInstallationLogRepository InstallationLogs { get; }

        // NEW
        public IPackageVersionRepository PackageVersions { get; }
        public IDeploymentHistoryRepository DeploymentHistories { get; }
        public IApplicationCategoryRepository ApplicationCategories { get; }
        public IDownloadStatisticRepository DownloadStatistics { get; }
        public IApplicationManifestRepository ApplicationManifests { get; }

        public UnitOfWork(
            DeploymentManagerDbContext context,
            IApplicationRepository applications,
            IInstallationLogRepository installationLogs,
            IPackageVersionRepository packageVersions,
            IDeploymentHistoryRepository deploymentHistories,
            IApplicationCategoryRepository applicationCategories,
            IDownloadStatisticRepository downloadStatistics,
            IApplicationManifestRepository applicationManifests)
        {
            _context = context;
            Applications = applications;
            InstallationLogs = installationLogs;
            PackageVersions = packageVersions;
            DeploymentHistories = deploymentHistories;
            ApplicationCategories = applicationCategories;
            DownloadStatistics = downloadStatistics;
            ApplicationManifests = applicationManifests;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}