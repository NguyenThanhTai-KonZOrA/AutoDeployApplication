namespace ClientLauncher.Implement.ViewModels.Response
{
    public class PermissionResponse
    {
        public int Id { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionCode { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}