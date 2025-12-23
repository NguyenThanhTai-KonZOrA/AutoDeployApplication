namespace ClientLauncher.Models
{
    public class VersionCheckDto
    {
        public string AppCode { get; set; } = string.Empty;
        public string BinaryVersion { get; set; } = string.Empty;
        public string ConfigVersion { get; set; } = string.Empty;
        public string UpdateType { get; set; } = "none";
        public bool ForceUpdate { get; set; }
    }

    public class VersionComparisonResult
    {
        public bool UpdateAvailable { get; set; }
        public bool ForceUpdate { get; set; }
        public string LocalVersion { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public string UpdateType { get; set; } = "none";
        public string Message { get; set; } = string.Empty;
    }
}