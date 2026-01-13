using  ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class DownloadStatistic : BaseEntity
    {
        public int Id { get; set; }
        public int PackageVersionId { get; set; }

        // Client Info
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;

        // Download Info
        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
        public long BytesDownloaded { get; set; }
        public int DurationSeconds { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // Client Version
        public string? ClientLauncherVersion { get; set; }
        public string? OsVersion { get; set; }

        // Navigation
        public PackageVersion PackageVersion { get; set; } = null!;
    }
}