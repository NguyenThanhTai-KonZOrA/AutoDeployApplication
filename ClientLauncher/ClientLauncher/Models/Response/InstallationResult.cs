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

        // New Manifest to verify after download
        public ManifestDto? UpdatedManifest { get; set; }

        // Backup path to rollback if verify fails
        public string? BackupPath { get; set; }

        // Temp path to verify before moving to App folder
        public string? TempAppPath { get; set; }
    }
}