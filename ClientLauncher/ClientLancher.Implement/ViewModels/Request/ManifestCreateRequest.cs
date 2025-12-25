namespace ClientLancher.Implement.ViewModels.Request
{
    public class ManifestCreateRequest
    {
        public string Version { get; set; } = string.Empty;

        // Binary Package
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;

        // Config Package (optional)
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string ConfigMergeStrategy { get; set; } = "preserveLocal";

        // Update Policy
        public string UpdateType { get; set; } = "both";
        public bool ForceUpdate { get; set; }

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsStable { get; set; } = true;
        public DateTime? PublishedAt { get; set; }
    }

    public class ManifestUpdateRequest
    {
        public string? BinaryVersion { get; set; }
        public string? BinaryPackage { get; set; }
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string? ConfigMergeStrategy { get; set; }
        public string? UpdateType { get; set; }
        public bool? ForceUpdate { get; set; }
        public string? ReleaseNotes { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStable { get; set; }
    }
}