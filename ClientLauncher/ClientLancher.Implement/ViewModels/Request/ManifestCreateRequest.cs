namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ManifestCreateRequest
    {
        public string Version { get; set; } = string.Empty;

        // Binary Package
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;
        public List<string> BinaryFiles { get; set; } = new(); //  Specific binary files

        // Config Package (optional)
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }

        /// <summary>
        /// "preserveLocal" | "replaceAll" | "selective" | "merge"
        /// </summary>
        public string ConfigMergeStrategy { get; set; } = "preserveLocal";

        //  Config file policies for selective update
        public List<ConfigFilePolicyRequest> ConfigFiles { get; set; } = new();

        // Update Policy
        public string UpdateType { get; set; } = "both"; // "both" | "binary" | "config" | "none"
        public bool ForceUpdate { get; set; }
        public bool NotifyUser { get; set; } = true;
        public bool AllowSkip { get; set; } = true;
        public string UpdateDescription { get; set; } = string.Empty;

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsStable { get; set; } = true;
        public DateTime? PublishedAt { get; set; }
    }

    public class ConfigFilePolicyRequest
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// "merge" | "preserve" | "replace"
        /// </summary>
        public string UpdatePolicy { get; set; } = "preserve";

        /// <summary>
        /// "server" | "local" - Priority in merge operations
        /// </summary>
        public string Priority { get; set; } = "local";
    }

    public class ManifestUpdateRequest
    {
        public string? Version { get; set; }

        // Binary
        public string? BinaryVersion { get; set; }
        public string? BinaryPackage { get; set; }
        public List<string>? BinaryFiles { get; set; }

        // Config
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string? ConfigMergeStrategy { get; set; }
        public List<ConfigFilePolicyRequest>? ConfigFiles { get; set; }

        // Update Policy
        public string? UpdateType { get; set; }
        public bool? ForceUpdate { get; set; }
        public bool? NotifyUser { get; set; }
        public bool? AllowSkip { get; set; }
        public string? UpdateDescription { get; set; }

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStable { get; set; }
    }
}