using ClientLauncher.Implement.EntityModels;
using Microsoft.AspNetCore.Http;

namespace ClientLauncher.Implement.ViewModels.Request
{
    public class CreateIconRequest
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
        public IconType Type { get; set; }
        public int? ReferenceId { get; set; }
    }
}