namespace ClientLancher.Implement.ViewModels.Response
{
    public class ManifestResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        // Binary Info
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;

        // Config Info
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string ConfigMergeStrategy { get; set; } = string.Empty;

        // Update Policy
        public string UpdateType { get; set; } = string.Empty;
        public bool ForceUpdate { get; set; }

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsActive { get; set; }
        public bool IsStable { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Response format matching the JSON manifest structure
    /// </summary>
    public class ManifestJsonResponse
    {
        public string AppCode { get; set; } = string.Empty;
        public BinaryInfo Binary { get; set; } = new();
        public ConfigInfo Config { get; set; } = new();
        public UpdatePolicyInfo UpdatePolicy { get; set; } = new();

        public class BinaryInfo
        {
            public string Version { get; set; } = string.Empty;
            public string Package { get; set; } = string.Empty;
        }

        public class ConfigInfo
        {
            public string Version { get; set; } = string.Empty;
            public string Package { get; set; } = string.Empty;
            public string MergeStrategy { get; set; } = string.Empty;
        }

        public class UpdatePolicyInfo
        {
            public string Type { get; set; } = string.Empty;
            public bool Force { get; set; }
        }
    }
}