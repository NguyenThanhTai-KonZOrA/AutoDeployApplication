namespace ClientLancher.Implement.EntityModels
{
    /// <summary>
    /// Quản lý categories cho applications
    /// </summary>
    public class ApplicationCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Cage", "HTR", "Finance"
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}