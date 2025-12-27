using Microsoft.AspNetCore.Http;

namespace ClientLancher.Implement.ViewModels.Request
{
    public class PackageUploadRequest
    {
        public int ApplicationId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string PackageType { get; set; } = "Binary"; // Binary, Config
        public IFormFile PackageFile { get; set; } = null!;
        public string? ReleaseNotes { get; set; } = string.Empty;
        public bool IsStable { get; set; } = true;
        public string? MinimumClientVersion { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public bool PublishImmediately { get; set; } = false;
    }
}