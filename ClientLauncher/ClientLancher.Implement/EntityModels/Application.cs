using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class Application : BaseEntity
    {
        public int Id { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public ApplicationCategory? Category { get; set; }

        // Navigation
        public ICollection<InstallationLog> InstallationLogs { get; set; } = new List<InstallationLog>();
        public ICollection<PackageVersion> PackageVersions { get; set; } = new List<PackageVersion>();
    }
}