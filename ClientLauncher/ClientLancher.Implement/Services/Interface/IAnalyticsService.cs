using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IAnalyticsService
    {
        Task<DashboardStatistics> GetDashboardStatisticsAsync();
        Task<IEnumerable<DownloadStatisticResponse>> GetRecentDownloadsAsync(int take = 50);
        Task<Dictionary<string, int>> GetDownloadsByDateAsync(int packageVersionId, int days = 30);
        Task<IEnumerable<TopApplicationDownload>> GetTopApplicationsAsync(int take = 10);
    }
}