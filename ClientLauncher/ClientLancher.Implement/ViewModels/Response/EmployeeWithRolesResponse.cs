namespace ClientLauncher.Implement.ViewModels.Response
{
    public class EmployeeWithRolesResponse
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public List<RoleResponse>? Roles { get; set; }
        public List<string>? Permissions { get; set; }
    }
}