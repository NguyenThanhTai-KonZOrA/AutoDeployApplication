using ClientLauncher.Common.BaseEntity;

namespace ClientLauncher.Implement.EntityModels
{
    public class DeploymentTask : BaseEntity
    {
        public int Id { get; set; }

        // Foreign Keys
        public int DeploymentHistoryId { get; set; }
        public int TargetMachineId { get; set; }
        public int PackageVersionId { get; set; }

        // Application Info
        public string AppCode { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        // Task Status
        public string Status { get; set; } = "Queued"; // Queued, InProgress, Completed, Failed, Cancelled
        public int Priority { get; set; } = 0; // Higher number = higher priority
        
        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledFor { get; set; } // When to execute (optional)
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Progress Tracking
        public int ProgressPercentage { get; set; } = 0;
        public string? CurrentStep { get; set; } // "Downloading", "Extracting", "Installing", etc.
        
        // Result
        public bool IsSuccess { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public string? ErrorStackTrace { get; set; }
        
        // Retry Logic
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public DateTime? NextRetryAt { get; set; }
        
        // Additional Info
        public string? DeploymentNotes { get; set; }
        public long? DownloadSizeBytes { get; set; }
        public TimeSpan? InstallDuration { get; set; }

        // Navigation Properties
        public DeploymentHistory DeploymentHistory { get; set; } = null!;
        public ClientMachine TargetMachine { get; set; } = null!;
        public PackageVersion PackageVersion { get; set; } = null!;
    }
}
