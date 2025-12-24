namespace ClientLancher.Implement.ViewModels.Response
{
    public class DeploymentProgressResponse
    {
        public int DeploymentId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Progress Details
        public int TotalTargets { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }

        // Calculated
        public int CompletedCount => SuccessCount + FailedCount;
        public int ProgressPercentage => TotalTargets > 0
            ? (int)((double)CompletedCount / TotalTargets * 100)
            : 0;
        public double SuccessRate => CompletedCount > 0
            ? (double)SuccessCount / CompletedCount * 100
            : 0;

        // Timing
        public DateTime StartedAt { get; set; }
        public DateTime? EstimatedCompletion { get; set; }
        public TimeSpan? ElapsedTime { get; set; }

        // Target Lists
        public List<DeploymentTargetStatus> TargetStatuses { get; set; } = new();
    }

    public class DeploymentTargetStatus
    {
        public string TargetName { get; set; } = string.Empty; // Machine or User name
        public string Status { get; set; } = string.Empty; // Pending, Success, Failed
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}