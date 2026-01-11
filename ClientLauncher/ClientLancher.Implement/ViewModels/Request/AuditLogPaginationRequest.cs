namespace ClientLancher.Implement.ViewModels.Request
{
    public class AuditLogPaginationRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? Take { get; set; }
        public int? Skip { get; set; }
        public bool? IsSuccess { get; set; }
        public string? EntityType { get; set; }
        public string? UserName { get; set; }
        public string? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}