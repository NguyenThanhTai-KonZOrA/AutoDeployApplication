using ClientLancher.Common.BaseEntity;

namespace ClientLancher.Implement.EntityModels
{
    public class Icons : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public IconType Type { get; set; }
        public int? ReferenceId { get; set; } // ApplicationId or CategoryId
    }

    public enum IconType
    {
        Application = 1,
        Category = 2
    }
}