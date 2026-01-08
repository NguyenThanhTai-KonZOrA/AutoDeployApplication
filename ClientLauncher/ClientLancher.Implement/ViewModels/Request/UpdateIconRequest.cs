using ClientLancher.Implement.EntityModels;
using Microsoft.AspNetCore.Http;

namespace ClientLancher.Implement.ViewModels.Request
{
    public class UpdateIconRequest
    {
        public string? Name { get; set; }
        public IconType? Type { get; set; }
        public int? ReferenceId { get; set; }
        public IFormFile? File { get; set; } = null!;
        public bool? IsActive { get; set; } = null!;
    }
}