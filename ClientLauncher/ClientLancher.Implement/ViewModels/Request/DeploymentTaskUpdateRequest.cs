namespace ClientLauncher.Implement.ViewModels.Request
{
    public class DeploymentTaskUpdateRequest
    {
        public int TaskId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string? CurrentStep { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public long? DownloadSizeBytes { get; set; }
    }
}
