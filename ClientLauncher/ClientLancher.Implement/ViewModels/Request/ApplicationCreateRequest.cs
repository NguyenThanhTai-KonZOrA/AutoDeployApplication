namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ApplicationCreateRequest
    {
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int? CategoryId { get; set; }
        public string? Developer { get; set; }
        public string? SupportEmail { get; set; }
        public string? DocumentationUrl { get; set; }
        public bool RequiresAdminRights { get; set; }
        public string? MinimumOsVersion { get; set; }
    }
}