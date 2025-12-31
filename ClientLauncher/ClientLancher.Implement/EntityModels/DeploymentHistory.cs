using ClientLancher.Common.BaseEntity;

namespace ClientLancher.Implement.EntityModels
{
    public class DeploymentHistory : BaseEntity
    {
        public int Id { get; set; }
        public int PackageVersionId { get; set; }

        // Deployment Info
        public string Environment { get; set; } = "Production"; // Dev, Staging, Production
        public string DeploymentType { get; set; } = "Release"; // Release, Hotfix, Rollback
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Success, Failed, Cancelled

        // Targeting
        public string? TargetMachines { get; set; } // JSON array của machine names/IDs
        public string? TargetUsers { get; set; } // JSON array của usernames
        public bool IsGlobalDeployment { get; set; } // Deploy for all machines/users

        // Progress
        public int TotalTargets { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }

        // Metadata
        public string DeployedBy { get; set; } = string.Empty;
        public DateTime DeployedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }

        // Approval (Optional)
        public bool RequiresApproval { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public PackageVersion PackageVersion { get; set; } = null!;
    }
}