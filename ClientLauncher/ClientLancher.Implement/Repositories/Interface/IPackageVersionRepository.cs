using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IPackageVersionRepository : IGenericRepository<PackageVersion>
    {
        Task<PackageVersion?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<PackageVersion>> GetByApplicationIdAsync(int applicationId);
        Task<PackageVersion?> GetByApplicationAndVersionAsync(int applicationId, string version);
        Task<PackageVersion?> GetLatestVersionAsync(int applicationId, bool stableOnly = true);
        Task<IEnumerable<PackageVersion>> GetVersionHistoryAsync(int applicationId, int take = 10);
        Task<IEnumerable<PackageVersion>> GetActiveVersionsAsync(int applicationId);
        Task<bool> VersionExistsAsync(int applicationId, string version);
        Task<IEnumerable<PackageVersion>> GetPendingPublishAsync();
        Task<long> GetTotalStorageSizeAsync(int? applicationId = null);
        void Delete(PackageVersion package);
    }
}