using  ClientLauncher.Common.Helper;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;

namespace ClientLauncher.Implement.Services
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
                var allCategories = (await _unitOfWork.ApplicationCategories.GetAllAsync()).ToList();

                var now = DateTime.UtcNow;
                var week = now.StartOfWeek(DayOfWeek.Monday);
                var todayDownloads = allDownloads.Count(d => d.DownloadedAt.Date == now.Date && d.Success);
                var weekDownloads = allDownloads.Count(d => d.DownloadedAt >= now.AddDays(-7) && d.Success);
                var monthDownloads = allDownloads.Count(d => d.DownloadedAt >= now.AddDays(-30) && d.Success);

                var pendingDeployments = (await _unitOfWork.DeploymentHistories.GetPendingDeploymentsAsync()).Count();
                var failedInstalls = allInstalls.Count(i => i.Status == "Failed");
                var successInstalls = allInstalls.Count(i => i.Status == "Success" && i.Action == "Install");
                var totalStorage = await _unitOfWork.PackageVersions.GetTotalStorageSizeAsync();

                var topApps = await GetTopApplicationsAsync(10);
                var recentActivities = await GetRecentActivitiesAsync(10);
                var categories = allCategories.Select(MapToResponse).OrderBy(x => x.DisplayOrder).ToList();

                // Calculate chart data
                var installationTrends = GetInstallationTrends(allInstalls, allApps);
                var topUpdateApps = GetTopUpdateApplications(allInstalls, allApps, allVersions);
                var mostActiveApps = GetMostActiveApplications(allInstalls, allApps);
                var monthlyComparison = GetMonthlyComparisonStats(allInstalls, allDownloads);

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
                    SuccessfulInstallations = successInstalls,
                    FailedInstallations = failedInstalls,
                    TopApplications = topApps.ToList(),
                    RecentActivities = recentActivities.ToList(),
                    Categories = categories,
                    InstallationTrends = installationTrends,
                    TopUpdateApplications = topUpdateApps,
                    MostActiveApplications = mostActiveApps,
                    MonthlyComparison = monthlyComparison
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
                    LatestVersion = latestVersion?.Version ?? string.Empty,
                    IconUrl = app.IconUrl
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

        private CategoryResponse MapToResponse(ApplicationCategory category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                DisplayName = category.DisplayName,
                Description = category.Description,
                IconUrl = category.IconUrl,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                ApplicationCount = category.Applications?.Count ?? 0
            };
        }

        private List<ApplicationInstallationTrend> GetInstallationTrends(List<InstallationLog> allInstalls, List<Application> allApps)
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var trends = allApps.Select(app =>
            {
                var appInstalls = allInstalls.Where(i => i.ApplicationId == app.Id && i.Action == "Install").ToList();
                var currentMonth = appInstalls.Count(i => i.StartedAt >= currentMonthStart);
                var previousMonth = appInstalls.Count(i => i.StartedAt >= previousMonthStart && i.StartedAt < currentMonthStart);

                var growth = currentMonth - previousMonth;
                var growthPercentage = previousMonth > 0 ? ((double)growth / previousMonth) * 100 : (currentMonth > 0 ? 100 : 0);

                return new ApplicationInstallationTrend
                {
                    AppCode = app.AppCode,
                    ApplicationName = app.Name,
                    CurrentMonthInstallations = currentMonth,
                    PreviousMonthInstallations = previousMonth,
                    GrowthCount = growth,
                    GrowthPercentage = Math.Round(growthPercentage, 2)
                };
            })
            .OrderByDescending(t => t.CurrentMonthInstallations)
            .Take(10)
            .ToList();

            return trends;
        }

        private List<ApplicationUpdateStats> GetTopUpdateApplications(List<InstallationLog> allInstalls, List<Application> allApps, List<PackageVersion> allVersions)
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);

            var updateStats = allApps.Select(app =>
            {
                var appUpdates = allInstalls.Where(i => i.ApplicationId == app.Id && i.Action == "Update").ToList();
                var totalUpdates = appUpdates.Count;
                var updatesThisMonth = appUpdates.Count(i => i.StartedAt >= currentMonthStart);
                var latestVersion = allVersions
                    .Where(v => v.ApplicationId == app.Id)
                    .OrderByDescending(v => v.UploadedAt)
                    .FirstOrDefault();
                var lastUpdate = appUpdates.OrderByDescending(i => i.StartedAt).FirstOrDefault();

                return new ApplicationUpdateStats
                {
                    AppCode = app.AppCode,
                    ApplicationName = app.Name,
                    TotalUpdates = totalUpdates,
                    UpdatesThisMonth = updatesThisMonth,
                    LatestVersion = latestVersion?.Version ?? string.Empty,
                    LastUpdateDate = lastUpdate?.StartedAt
                };
            })
            .OrderByDescending(s => s.TotalUpdates)
            .Take(10)
            .ToList();

            return updateStats;
        }

        private List<ApplicationActivityStats> GetMostActiveApplications(List<InstallationLog> allInstalls, List<Application> allApps)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var activityStats = allApps.Select(app =>
            {
                var appInstalls = allInstalls.Where(i => i.ApplicationId == app.Id).ToList();

                // Count unique machines for each period
                var allMachines = appInstalls
                    .Select(i => string.IsNullOrEmpty(i.MachineId) ? i.MachineName : i.MachineId)
                    .Distinct()
                    .Count();

                var todayMachines = appInstalls
                    .Where(i => i.StartedAt.Date == today)
                    .Select(i => string.IsNullOrEmpty(i.MachineId) ? i.MachineName : i.MachineId)
                    .Distinct()
                    .Count();

                var weekMachines = appInstalls
                    .Where(i => i.StartedAt >= weekAgo)
                    .Select(i => string.IsNullOrEmpty(i.MachineId) ? i.MachineName : i.MachineId)
                    .Distinct()
                    .Count();

                var monthMachines = appInstalls
                    .Where(i => i.StartedAt >= monthAgo)
                    .Select(i => string.IsNullOrEmpty(i.MachineId) ? i.MachineName : i.MachineId)
                    .Distinct()
                    .Count();

                var lastActivity = appInstalls.OrderByDescending(i => i.StartedAt).FirstOrDefault();

                return new ApplicationActivityStats
                {
                    AppCode = app.AppCode,
                    ApplicationName = app.Name,
                    TotalActiveMachines = allMachines,
                    TodayActiveMachines = todayMachines,
                    WeekActiveMachines = weekMachines,
                    MonthActiveMachines = monthMachines,
                    LastActivityDate = lastActivity?.StartedAt
                };
            })
            .OrderByDescending(s => s.MonthActiveMachines)
            .Take(10)
            .ToList();

            return activityStats;
        }

        private MonthlyComparisonStats GetMonthlyComparisonStats(List<InstallationLog> allInstalls, List<DownloadStatistic> allDownloads)
        {
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var currentMonthInstalls = allInstalls.Count(i => i.StartedAt >= currentMonthStart && i.Action == "Install");
            var previousMonthInstalls = allInstalls.Count(i => i.StartedAt >= previousMonthStart && i.StartedAt < currentMonthStart && i.Action == "Install");

            var currentMonthDownloads = allDownloads.Count(d => d.DownloadedAt >= currentMonthStart && d.Success);
            var previousMonthDownloads = allDownloads.Count(d => d.DownloadedAt >= previousMonthStart && d.DownloadedAt < currentMonthStart && d.Success);

            var currentMonthActiveApps = allInstalls
                .Where(i => i.StartedAt >= currentMonthStart)
                .Select(i => i.ApplicationId)
                .Distinct()
                .Count();

            var previousMonthActiveApps = allInstalls
                .Where(i => i.StartedAt >= previousMonthStart && i.StartedAt < currentMonthStart)
                .Select(i => i.ApplicationId)
                .Distinct()
                .Count();

            var installGrowth = previousMonthInstalls > 0 
                ? ((double)(currentMonthInstalls - previousMonthInstalls) / previousMonthInstalls) * 100 
                : (currentMonthInstalls > 0 ? 100 : 0);

            var downloadGrowth = previousMonthDownloads > 0 
                ? ((double)(currentMonthDownloads - previousMonthDownloads) / previousMonthDownloads) * 100 
                : (currentMonthDownloads > 0 ? 100 : 0);

            return new MonthlyComparisonStats
            {
                CurrentMonthInstallations = currentMonthInstalls,
                PreviousMonthInstallations = previousMonthInstalls,
                CurrentMonthDownloads = currentMonthDownloads,
                PreviousMonthDownloads = previousMonthDownloads,
                CurrentMonthActiveApps = currentMonthActiveApps,
                PreviousMonthActiveApps = previousMonthActiveApps,
                InstallationGrowthPercentage = Math.Round(installGrowth, 2),
                DownloadGrowthPercentage = Math.Round(downloadGrowth, 2)
            };
        }
    }
}