using  ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class PackageVersion : BaseEntity
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        // Version Info
        public string Version { get; set; } = string.Empty; // "1.0.0"
        public string PackageFileName { get; set; } = string.Empty; // "APP001_v1.0.0.zip"
        public string PackageType { get; set; } = "Binary"; // Binary, Config

        // File Info
        public long FileSizeBytes { get; set; }
        public string FileHash { get; set; } = string.Empty; // SHA256
        public string StoragePath { get; set; } = string.Empty; // Relative path: "APP001/1.0.0/APP001_v1.0.0.zip"

        // Metadata
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsStable { get; set; } = true; // Stable/Beta/Alpha
        public string? MinimumClientVersion { get; set; } // requires client version >= this

        // Upload Info
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }

        // Download Stats
        public int DownloadCount { get; set; }
        public DateTime? LastDownloadedAt { get; set; }

        // Rollback Support
        public int? ReplacesVersionId { get; set; } // Version replaced by this version
        public PackageVersion? ReplacesVersion { get; set; }

        // Navigation
        public Application Application { get; set; } = null!;
        public ICollection<DeploymentHistory> DeploymentHistories { get; set; } = new List<DeploymentHistory>();
        public ICollection<DownloadStatistic> DownloadStatistics { get; set; } = new List<DownloadStatistic>();
    }
}