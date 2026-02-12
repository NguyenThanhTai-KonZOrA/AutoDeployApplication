namespace ClientLauncher.Implement.ViewModels
{
    public class ApplicationSettingDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public string DataType { get; set; } = "String";
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class CreateSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "System";
        public string DataType { get; set; } = "String";
    }

    public class UpdateSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class BulkUpdateSettingsRequest
    {
        public List<UpdateSettingRequest> Settings { get; set; } = new();
    }
}
