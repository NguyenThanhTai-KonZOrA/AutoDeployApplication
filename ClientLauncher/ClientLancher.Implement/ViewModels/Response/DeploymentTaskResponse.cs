namespace ClientLauncher.Implement.ViewModels.Response
{
    public class DeploymentTaskResponse
    {
        public int Id { get; set; }
        public int DeploymentHistoryId { get; set; }
        public int TargetMachineId { get; set; }
        public string TargetMachineName { get; set; } = string.Empty;
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int ProgressPercentage { get; set; }
        public string? CurrentStep { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan? InstallDuration { get; set; }
    }
}
