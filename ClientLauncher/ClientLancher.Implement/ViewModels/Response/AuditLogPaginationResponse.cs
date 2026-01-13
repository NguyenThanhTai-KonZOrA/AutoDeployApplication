namespace ClientLauncher.Implement.ViewModels.Response
{
    public class AuditLogPaginationResponse
    {
        public List<AuditLogResponse> Logs { get; set; } = new List<AuditLogResponse>();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalUsedApplications { get; set; }
    }
}