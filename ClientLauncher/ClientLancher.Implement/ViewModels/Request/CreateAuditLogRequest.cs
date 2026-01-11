namespace ClientLancher.Implement.ViewModels.Request
{
    public class CreateAuditLogRequest
    {
        public string UserName { get; set; } = "Anonymous";
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; } = true;
        public int? StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public long? DurationMs { get; set; }
    }
}