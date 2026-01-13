using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.ViewModels.Request
{
    public class AppManifest
    {
        public string appCode { get; set; } = string.Empty;
        public BinaryInfo binary { get; set; } = new();
        public ConfigInfo config { get; set; } = new();
        public UpdatePolicy updatePolicy { get; set; } = new();
    }

    public class BinaryInfo
    {
        public string version { get; set; } = string.Empty;
        public string package { get; set; } = string.Empty;
    }

    public class ConfigInfo
    {
        public string version { get; set; } = string.Empty;
        public string package { get; set; } = string.Empty;
        public string mergeStrategy { get; set; } = "preserveLocal"; // preserveLocal, replaceAll
    }

    public class UpdatePolicy
    {
        public string type { get; set; } = "both"; // both, config, binary, none
        public bool force { get; set; } = false;
    }

    public class ManifestVersionReponse
    {
        public int status { get; set; }
        public AppManifest data { get; set; }
        public bool success { get; set; }
    }
}