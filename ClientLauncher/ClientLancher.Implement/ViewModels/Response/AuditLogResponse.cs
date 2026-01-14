namespace ClientLauncher.Implement.ViewModels.Response
{
    public class AuditLogResponse
    {
        public int ID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }
        public int? StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public long? DurationMs { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogPagedResponse
    {
        public List<AuditLogResponse> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
