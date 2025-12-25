using ClientLancher.Implement.Repositories.Interface;

namespace ClientLancher.Implement.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IApplicationRepository Applications { get; }
        IInstallationLogRepository InstallationLogs { get; }
        IPackageVersionRepository PackageVersions { get; }
        IDeploymentHistoryRepository DeploymentHistories { get; }
        IApplicationCategoryRepository ApplicationCategories { get; }
        IDownloadStatisticRepository DownloadStatistics { get; }
        IApplicationManifestRepository ApplicationManifests { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}