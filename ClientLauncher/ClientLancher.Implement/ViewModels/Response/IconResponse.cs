using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.ViewModels.Response
{
    public class IconResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public IconType Type { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

    }
}