namespace ClientLauncher.Models
{
    public class VersionInfoDto
    {
        public string AppCode { get; set; } = string.Empty;
        public string BinaryVersion { get; set; } = string.Empty;
        public string ConfigVersion { get; set; } = string.Empty;
        public string UpdateType { get; set; } = string.Empty;
        public bool ForceUpdate { get; set; }
    }
}