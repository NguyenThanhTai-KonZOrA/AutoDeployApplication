using ClientLauncher.Services;

namespace ClientLauncher.Models
{
    // DTOs
    public class ManifestDto
    {
        public string AppCode { get; set; } = string.Empty;
        public BinaryDto? Binary { get; set; }
        public ConfigDto? Config { get; set; }
        public UpdatePolicyDto? UpdatePolicy { get; set; }
    }

    public class BinaryDto
    {
        public string Version { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
    }

    public class ConfigDto
    {
        public string Version { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
        public string MergeStrategy { get; set; } = "preserveLocal";
    }

    public class UpdatePolicyDto
    {
        public string Type { get; set; } = "both";
        public bool Force { get; set; }
    }
}
