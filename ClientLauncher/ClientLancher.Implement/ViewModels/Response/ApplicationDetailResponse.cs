namespace ClientLancher.Implement.ViewModels.Response
{
    public class ApplicationDetailResponse
    {
        public int Id { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public bool IsActive { get; set; }
        public string? Developer { get; set; }
        public string? SupportEmail { get; set; }
        public string? DocumentationUrl { get; set; }
        public bool RequiresAdminRights { get; set; }
        public string? MinimumOsVersion { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Latest Version Info
        public string? LatestVersion { get; set; }
        public DateTime? LatestVersionDate { get; set; }
        public int TotalVersions { get; set; }
        public int TotalInstalls { get; set; }
        public long TotalStorageSize { get; set; }
    }
}