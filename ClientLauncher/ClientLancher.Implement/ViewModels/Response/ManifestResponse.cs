using System.Text.Json;

namespace ClientLauncher.Implement.ViewModels.Response
{
    public class ManifestResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        // Binary
        public string BinaryVersion { get; set; } = string.Empty;
        public string BinaryPackage { get; set; } = string.Empty;
        public List<string> BinaryFiles { get; set; } = new();

        // Config
        public string? ConfigVersion { get; set; }
        public string? ConfigPackage { get; set; }
        public string ConfigMergeStrategy { get; set; } = "preserveLocal";
        public List<ConfigFilePolicyResponse> ConfigFiles { get; set; } = new();

        // Update Policy
        public string UpdateType { get; set; } = "both";
        public bool ForceUpdate { get; set; }
        public bool NotifyUser { get; set; } = true;
        public bool AllowSkip { get; set; } = true;
        public string UpdateDescription { get; set; } = string.Empty;

        // Metadata
        public string? ReleaseNotes { get; set; }
        public bool IsActive { get; set; }
        public bool IsStable { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class ConfigFilePolicyResponse
    {
        public string Name { get; set; } = string.Empty;
        public string UpdatePolicy { get; set; } = "preserve";
        public string Priority { get; set; } = "local";
    }

    /// <summary>
    /// JSON format for client download
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
            public List<string> Files { get; set; } = new();
        }

        public class ConfigInfo
        {
            public string Version { get; set; } = string.Empty;
            public string Package { get; set; } = string.Empty;
            public string MergeStrategy { get; set; } = "preserveLocal";
            public List<ConfigFilePolicy> Files { get; set; } = new();
        }

        public class ConfigFilePolicy
        {
            public string Name { get; set; } = string.Empty;
            public string UpdatePolicy { get; set; } = "preserve";
            public string Priority { get; set; } = "local";
        }

        public class UpdatePolicyInfo
        {
            public string Type { get; set; } = "both";
            public bool Force { get; set; }
            public bool NotifyUser { get; set; } = true;
            public bool AllowSkip { get; set; } = true;
            public string Description { get; set; } = string.Empty;
        }
    }
}