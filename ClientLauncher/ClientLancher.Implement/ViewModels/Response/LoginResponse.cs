namespace ClientLauncher.Implement.ViewModels.Response
{
    public class LoginResponse
    {
        public string UserName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string[] Role { get; set; } = Array.Empty<string>();
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
    }
}