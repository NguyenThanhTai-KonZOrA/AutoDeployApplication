
using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class ApplicationCategory : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Cage", "HTR", "Finance"
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int DisplayOrder { get; set; }
        // Navigation
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}