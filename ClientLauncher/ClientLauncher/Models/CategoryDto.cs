namespace ClientLauncher.Models
{
    /// <summary>
    /// Maps to CategoryResponse from ClientLauncherAPI
    /// </summary>
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ApplicationCount { get; set; }
    }
}