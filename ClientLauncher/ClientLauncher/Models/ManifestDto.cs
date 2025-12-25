namespace ClientLauncher.Models
{
    /// <summary>
    /// DTO for manifest data matching server response
    /// </summary>
    public class ManifestDto
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
            public string MergeStrategy { get; set; } = "preserveLocal";
        }

        public class UpdatePolicyInfo
        {
            public string Type { get; set; } = "both"; // binary, config, both, none
            public bool Force { get; set; }
        }
    }
}