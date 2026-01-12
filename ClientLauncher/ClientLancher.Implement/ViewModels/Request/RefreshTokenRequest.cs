using System.ComponentModel.DataAnnotations;

namespace ClientLancher.Implement.ViewModels.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}