namespace ClientLauncher.Implement.ViewModels.Request
{
    public class ApprovalRequest
    {
        public string ApprovedBy { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }
}