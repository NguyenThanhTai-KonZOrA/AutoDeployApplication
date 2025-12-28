namespace ClientLauncher.Models
{
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
            public List<string> Files { get; set; } = new(); // NEW: Specific files to update
        }

        public class ConfigInfo
        {
            public string Version { get; set; } = string.Empty;
            public string Package { get; set; } = string.Empty;

            /// <summary>
            /// Strategy: "preserveLocal" | "replaceAll" | "selective" | "merge"
            /// </summary>
            public string MergeStrategy { get; set; } = "preserveLocal";

            // NEW: Selective file update configuration
            public List<ConfigFilePolicy> Files { get; set; } = new();
        }

        public class ConfigFilePolicy
        {
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// "merge" | "preserve" | "replace"
            /// </summary>
            public string UpdatePolicy { get; set; } = "preserve";

            /// <summary>
            /// "server" | "local" - Which takes priority in merge
            /// </summary>
            public string Priority { get; set; } = "local";
        }

        public class UpdatePolicyInfo
        {
            /// <summary>
            /// "both" | "binary" | "config" | "none"
            /// </summary>
            public string Type { get; set; } = "both";

            public bool Force { get; set; }

            // NEW: Additional flags
            public bool NotifyUser { get; set; } = true;
            public bool AllowSkip { get; set; } = true;
            public string Description { get; set; } = string.Empty;
        }
    }
}