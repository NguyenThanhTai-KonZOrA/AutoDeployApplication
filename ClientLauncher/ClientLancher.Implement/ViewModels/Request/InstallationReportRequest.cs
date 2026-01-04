namespace ClientLancher.Implement.ViewModels.Request
{
    public class InstallationReportRequest
    {
        public int? ApplicationId { get; set; }
        public string? MachineName { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}