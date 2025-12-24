using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IDownloadStatisticRepository : IGenericRepository<DownloadStatistic>
    {
        Task<IEnumerable<DownloadStatistic>> GetByPackageVersionIdAsync(int packageVersionId);
        Task<IEnumerable<DownloadStatistic>> GetByMachineAsync(string machineName);
        Task<int> GetTotalDownloadsAsync(int packageVersionId);
        Task<IEnumerable<DownloadStatistic>> GetRecentDownloadsAsync(int take = 50);
        Task<Dictionary<string, int>> GetDownloadsByDateAsync(int packageVersionId, int days = 30);
    }
}