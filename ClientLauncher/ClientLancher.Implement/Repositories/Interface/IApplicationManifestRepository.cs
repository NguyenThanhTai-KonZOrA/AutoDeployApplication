using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IApplicationManifestRepository : IGenericRepository<ApplicationManifest>
    {
        Task<ApplicationManifest?> GetLatestActiveManifestAsync(int applicationId);
        Task<ApplicationManifest?> GetByVersionAsync(int applicationId, string version);
        Task<IEnumerable<ApplicationManifest>> GetManifestHistoryAsync(int applicationId, int take = 10);
        Task<ApplicationManifest?> GetLatestActiveManifestByAppCodeAsync(string appCode);
        Task<bool> VersionExistsAsync(int applicationId, string version);
        Task DeactivateAllManifestsAsync(int applicationId);
    }
}