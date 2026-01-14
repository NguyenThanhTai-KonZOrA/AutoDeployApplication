namespace ClientLauncher.Implement.ViewModels.Response
{
    public class DownloadStatisticResponse
    {
        public int Id { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;

        public DateTime DownloadedAt { get; set; }
        public long BytesDownloaded { get; set; }
        public int DurationSeconds { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public string? ClientLauncherVersion { get; set; }
        public string? OsVersion { get; set; }
    }
}