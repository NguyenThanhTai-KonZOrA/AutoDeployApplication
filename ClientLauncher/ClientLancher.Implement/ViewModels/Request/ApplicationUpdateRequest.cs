namespace ClientLancher.Implement.ViewModels.Request
{
    public class ApplicationUpdateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public string? Developer { get; set; }
        public string? SupportEmail { get; set; }
        public string? DocumentationUrl { get; set; }
        public bool? RequiresAdminRights { get; set; }
        public string? MinimumOsVersion { get; set; }
    }
}