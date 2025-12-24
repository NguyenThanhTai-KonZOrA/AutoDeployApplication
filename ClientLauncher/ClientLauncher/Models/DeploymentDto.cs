namespace ClientLauncher.Models
{
    /// <summary>
    /// Maps to DeploymentResponse from ClientLauncherAPI
    /// </summary>
    public class DeploymentDto
    {
        public int Id { get; set; }
        public int PackageVersionId { get; set; }
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public string Environment { get; set; } = string.Empty;
        public string DeploymentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public bool IsGlobalDeployment { get; set; }
        public int TotalTargets { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int ProgressPercentage { get; set; }

        public string DeployedBy { get; set; } = string.Empty;
        public DateTime DeployedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }

        public bool RequiresApproval { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}