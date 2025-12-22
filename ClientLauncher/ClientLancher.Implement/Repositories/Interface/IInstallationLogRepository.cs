using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IInstallationLogRepository : IGenericRepository<InstallationLog>
    {
        Task<IEnumerable<InstallationLog>> GetByApplicationIdAsync(int applicationId);
        Task<IEnumerable<InstallationLog>> GetByUserNameAsync(string userName);
        Task<IEnumerable<InstallationLog>> GetByMachineNameAsync(string machineName);
        Task<IEnumerable<InstallationLog>> GetFailedInstallationsAsync();
        Task<InstallationLog?> GetLatestByAppCodeAsync(string appCode);
        Task<IEnumerable<InstallationLog>> GetInstallationHistoryAsync(string appCode, int take = 10);
    }
}