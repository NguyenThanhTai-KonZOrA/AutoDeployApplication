namespace ClientLauncher.Implement.ViewModels.Response
{
    public class InstallationReportResponse
    {
        public int ApplicationId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;
        public List<VersionInstallationStats> VersionStats { get; set; } = new();
        public int TotalPCs { get; set; }
    }

    public class VersionInstallationStats
    {
        public string Version { get; set; } = string.Empty;
        public int PCCount { get; set; }
        public int UpdatedPCCount { get; set; }
        public int NotUpdatedPCCount { get; set; }
        public List<PCInstallationDetail> PCs { get; set; } = new();
    }

    public class PCInstallationDetail
    {
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}