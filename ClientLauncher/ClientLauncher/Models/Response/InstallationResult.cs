namespace ClientLauncher.Models.Response
{
    public class InstallationResult
    {
        public string AppCode { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public string? InstalledVersion { get; set; }
        public string? InstallationPath { get; set; }
        public ManifestDto? UpdatedManifest { get; set; }
    }
}