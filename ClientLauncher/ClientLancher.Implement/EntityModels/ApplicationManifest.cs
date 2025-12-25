using ClientLancher.Common.BaseEntity;

namespace ClientLancher.Implement.EntityModels
{
    /// <summary>
    /// Represents a manifest configuration for an application version
    /// </summary>
    public class ApplicationManifest : BaseEntity
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string Version { get; set; } = string.Empty;

        // Binary Package Info
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;

        // Config Package Info
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string ConfigMergeStrategy { get; set; } = "preserveLocal"; // preserveLocal, overwrite, merge

        // Update Policy
        public string UpdateType { get; set; } = "both"; // binary, config, both, none
        public bool ForceUpdate { get; set; }

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsStable { get; set; } = true;
        public DateTime? PublishedAt { get; set; }

        // Navigation property
        public virtual Application Application { get; set; } = null!;
    }
}