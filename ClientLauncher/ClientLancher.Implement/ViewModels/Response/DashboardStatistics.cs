namespace ClientLancher.Implement.ViewModels.Response
{
    public class DashboardStatistics
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

        public List<TopApplicationDownload> TopApplications { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();

        public List<CategoryResponse> Categories { get; set; } = new();
    }

    public class TopApplicationDownload
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty; // Upload, Download, Deployment, Installation
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}