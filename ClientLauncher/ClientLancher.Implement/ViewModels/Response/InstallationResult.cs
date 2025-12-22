namespace ClientLancher.Implement.ViewModels.Response
{
    public class InstallationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public string? InstalledVersion { get; set; }
    }
}
