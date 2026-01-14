namespace ClientLauncher.Models
{
    /// <summary>
    /// Maps to DashboardStatistics from ClientLauncherAPI
    /// </summary>
    public class DashboardStatisticsDto
    {
        public int TotalApplications { get; set; }
        public int ActiveApplications { get; set; }
        public int TotalVersions { get; set; }
        public int TotalInstallations { get; set; }

        public long TotalStorageUsed { get; set; }
        public string TotalStorageFormatted { get; set; } = string.Empty;

        public int TodayDownloads { get; set; }
        public int WeekDownloads { get; set; }
        public int MonthDownloads { get; set; }

        public int PendingDeployments { get; set; }
        public int FailedInstallations { get; set; }

        public List<TopApplicationDownloadDto> TopApplications { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class TopApplicationDownloadDto
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
    }

    public class RecentActivityDto
    {
        public string Type { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}