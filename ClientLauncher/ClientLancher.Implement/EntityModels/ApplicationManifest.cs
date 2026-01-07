using ClientLancher.Common.BaseEntity;

namespace ClientLancher.Implement.EntityModels
{
    public class ApplicationManifest : BaseEntity
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string Version { get; set; } = string.Empty;

        // Binary Information
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;
        public string? BinaryFilesJson { get; set; } //  JSON array of specific files

        // Config Information
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string ConfigMergeStrategy { get; set; } = "preserveLocal";
        public string? ConfigFilesJson { get; set; } //  JSON array of config file policies

        // Update Policy
        public string UpdateType { get; set; } = "both";
        public bool ForceUpdate { get; set; }
        public bool NotifyUser { get; set; } = true;
        public bool AllowSkip { get; set; } = true;
        public string UpdateDescription { get; set; } = string.Empty;

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsStable { get; set; } = true;
        public DateTime? PublishedAt { get; set; }

        // Navigation
        public Application Application { get; set; } = null!;
    }
}