namespace ClientLauncher.Models
{
    public class InstallationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public string? InstalledVersion { get; set; }
    }
}