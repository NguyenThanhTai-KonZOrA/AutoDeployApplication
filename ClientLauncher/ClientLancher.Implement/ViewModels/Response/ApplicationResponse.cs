namespace ClientLancher.Implement.ViewModels.Response
{
    public class ApplicationResponse
    {
        public int Id { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Properties for version tracking
        public bool IsInstalled { get; set; }
        public string? InstalledVersion { get; set; }
        public string? ServerVersion { get; set; }
        public bool HasUpdate { get; set; }
        public string StatusText { get; set; } = "Not Installed";
    }
}