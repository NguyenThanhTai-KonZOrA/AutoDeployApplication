namespace ClientLancher.Implement.EntityModels
{
    public class Application
    {
        public int Id { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;

        // ✅ UPDATE: Link to Category
        public int? CategoryId { get; set; }
        public ApplicationCategory? Category { get; set; }

        public bool IsActive { get; set; } = true;

        // ✅ NEW: Additional Metadata
        public string? Developer { get; set; }
        public string? SupportEmail { get; set; }
        public string? DocumentationUrl { get; set; }
        public bool RequiresAdminRights { get; set; }
        public string? MinimumOsVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<InstallationLog> InstallationLogs { get; set; } = new List<InstallationLog>();

        // ✅ NEW
        public ICollection<PackageVersion> PackageVersions { get; set; } = new List<PackageVersion>();
    }
}