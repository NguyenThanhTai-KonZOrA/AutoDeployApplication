using ClientLauncher.Common.BaseEntity;
using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.EntityModels
{
    public class ApplicationSettings : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Value { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string Category { get; set; } = "General";

        [StringLength(50)]
        public string DataType { get; set; } = "String"; // String, Integer, Boolean, Json
    }
}