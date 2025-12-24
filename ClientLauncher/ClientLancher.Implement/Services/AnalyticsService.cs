using ClientLancher.Implement.Services.Interface;
using ClientLancher.Implement.UnitOfWork;
using ClientLancher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLancher.Implement.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IUnitOfWork unitOfWork, ILogger<AnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            try
            {
                var allApps = (await _unitOfWork.Applications.GetAllAsync()).ToList();
                var activeApps = allApps.Where(a => a.IsActive).ToList();
                var allVersions = (await _unitOfWork.PackageVersions.GetAllAsync()).ToList();
                var allInstalls = (await _unitOfWork.InstallationLogs.GetAllAsync()).ToList();
                var allDownloads = (await _unitOfWork.DownloadStatistics.GetAllAsync()).ToList();

                var now = DateTime.UtcNow;
                var todayDownloads = allDownloads.Count(d => d.DownloadedAt.Date == now.Date && d.Success);
                var weekDownloads = allDownloads.Count(d => d.DownloadedAt >= now.AddDays(-7) && d.Success);
                var monthDownloads = allDownloads.Count(d => d.DownloadedAt >= now.AddDays(-30) && d.Success);

                var pendingDeployments = (await _unitOfWork.DeploymentHistories.GetPendingDeploymentsAsync()).Count();
                var failedInstalls = allInstalls.Count(i => i.Status == "Failed");

                var totalStorage = await _unitOfWork.PackageVersions.GetTotalStorageSizeAsync();

                var topApps = await GetTopApplicationsAsync(5);
                var recentActivities = await GetRecentActivitiesAsync(10);

                return new DashboardStatistics
                {
                    TotalApplications = allApps.Count,
                    ActiveApplications = activeApps.Count,
                    TotalVersions = allVersions.Count,
                    TotalInstallations = allInstalls.Count,
                    TotalStorageUsed = totalStorage,
                    TotalStorageFormatted = FormatFileSize(totalStorage),
                    TodayDownloads = todayDownloads,
                    WeekDownloads = weekDownloads,
                    MonthDownloads = monthDownloads,
                    PendingDeployments = pendingDeployments,
                    FailedInstallations = failedInstalls,
                    TopApplications = topApps.ToList(),
                    RecentActivities = recentActivities.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                throw;
            }
        }

        public async Task<IEnumerable<DownloadStatisticResponse>> GetRecentDownloadsAsync(int take = 50)
        {
            var downloads = await _unitOfWork.DownloadStatistics.GetRecentDownloadsAsync(take);

            return downloads.Select(d => new DownloadStatisticResponse
            {
                Id = d.Id,
                ApplicationName = d.PackageVersion.Application.Name,
                Version = d.PackageVersion.Version,
                MachineName = d.MachineName,
                UserName = d.UserName,
                IpAddress = d.IpAddress,
                DownloadedAt = d.DownloadedAt,
                BytesDownloaded = d.BytesDownloaded,
                DurationSeconds = d.DurationSeconds,
                Success = d.Success,
                ErrorMessage = d.ErrorMessage,
                ClientLauncherVersion = d.ClientLauncherVersion,
                OsVersion = d.OsVersion
            });
        }

        public async Task<Dictionary<string, int>> GetDownloadsByDateAsync(int packageVersionId, int days = 30)
        {
            return await _unitOfWork.DownloadStatistics.GetDownloadsByDateAsync(packageVersionId, days);
        }

        public async Task<IEnumerable<TopApplicationDownload>> GetTopApplicationsAsync(int take = 10)
        {
            var apps = await _unitOfWork.Applications.GetAllAsync();
            var topApps = new List<TopApplicationDownload>();

            foreach (var app in apps)
            {
                var versions = await _unitOfWork.PackageVersions.GetByApplicationIdAsync(app.Id);
                var totalDownloads = versions.Sum(v => v.DownloadCount);
                var latestVersion = await _unitOfWork.PackageVersions.GetLatestVersionAsync(app.Id);

                topApps.Add(new TopApplicationDownload
                {
                    AppCode = app.AppCode,
                    ApplicationName = app.Name,
                    DownloadCount = totalDownloads,
                    LatestVersion = latestVersion?.Version ?? "N/A"
                });
            }

            return topApps.OrderByDescending(a => a.DownloadCount).Take(take);
        }

        private async Task<IEnumerable<RecentActivity>> GetRecentActivitiesAsync(int take)
        {
            var activities = new List<RecentActivity>();

            // Get recent uploads
            var recentVersions = (await _unitOfWork.PackageVersions.GetAllAsync())
                .OrderByDescending(v => v.UploadedAt)
                .Take(take)
                .ToList();

            foreach (var version in recentVersions)
            {
                activities.Add(new RecentActivity
                {
                    Type = "Upload",
                    ApplicationName = version.Application.Name,
                    Version = version.Version,
                    User = version.UploadedBy,
                    Timestamp = version.UploadedAt,
                    Status = version.PublishedAt.HasValue ? "Published" : "Pending"
                });
            }

            // Get recent installations
            var recentInstalls = (await _unitOfWork.InstallationLogs.GetAllAsync())
                .OrderByDescending(i => i.StartedAt)
                .Take(take)
                .ToList();

            foreach (var install in recentInstalls)
            {
                activities.Add(new RecentActivity
                {
                    Type = "Installation",
                    ApplicationName = install.Application.Name,
                    Version = install.NewVersion,
                    User = install.UserName,
                    Timestamp = install.StartedAt,
                    Status = install.Status
                });
            }

            return activities.OrderByDescending(a => a.Timestamp).Take(take);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}