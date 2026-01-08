using Microsoft.AspNetCore.Http;

namespace ClientLancher.Implement.ViewModels.Request
{
    public class PackageUpdateRequest
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string? ReleaseNotes { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsStable { get; set; }
        public string? MinimumClientVersion { get; set; }
        public IFormFile? NewPackage { get; set; }
    }
}