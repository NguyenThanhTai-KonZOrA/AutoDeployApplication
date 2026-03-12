namespace ClientLauncher.Implement.ViewModels.Response
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
        public int SuccessfulInstallations { get; set; }
        public int FailedInstallations { get; set; }

        public List<TopApplicationDownload> TopApplications { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();

        public List<CategoryResponse> Categories { get; set; } = new();

        // Chart data for UI
        public List<ApplicationInstallationTrend> InstallationTrends { get; set; } = new();
        public List<ApplicationUpdateStats> TopUpdateApplications { get; set; } = new();
        public List<ApplicationActivityStats> MostActiveApplications { get; set; } = new();
        public MonthlyComparisonStats MonthlyComparison { get; set; } = new();
    }

    public class TopApplicationDownload
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string IconUrl { get; set; }
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

    public class ApplicationInstallationTrend
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int CurrentMonthInstallations { get; set; }
        public int PreviousMonthInstallations { get; set; }
        public int GrowthCount { get; set; }
        public double GrowthPercentage { get; set; }
    }

    public class ApplicationUpdateStats
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int TotalUpdates { get; set; }
        public int UpdatesThisMonth { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public DateTime? LastUpdateDate { get; set; }
    }

    public class ApplicationActivityStats
    {
        public string AppCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public int TotalActiveMachines { get; set; }
        public int TodayActiveMachines { get; set; }
        public int WeekActiveMachines { get; set; }
        public int MonthActiveMachines { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }

    public class MonthlyComparisonStats
    {
        public int CurrentMonthInstallations { get; set; }
        public int PreviousMonthInstallations { get; set; }
        public int CurrentMonthDownloads { get; set; }
        public int PreviousMonthDownloads { get; set; }
        public int CurrentMonthActiveApps { get; set; }
        public int PreviousMonthActiveApps { get; set; }
        public double InstallationGrowthPercentage { get; set; }
        public double DownloadGrowthPercentage { get; set; }
    }
}