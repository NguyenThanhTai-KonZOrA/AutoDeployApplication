export interface DeploymentCreateRequest {
    PackageVersionId: number;
    Environment: string; // Production, Staging, Development
    DeploymentType: string; // Release, Hotfix, Rollback
    IsGlobalDeployment: boolean;
    TargetMachines: string[];
    TargetUsers: string[];
    RequiresApproval: boolean;
    ScheduledFor: string | null;
}

export interface DeploymentResponse {
    id: string;
    packageVersionId: number;
    applicationName: string;
    version: string;
    environment: string; // Production, Staging, Development
    deploymentType: string; // Release, Hotfix, Rollback
    status: string; // Pending, Pending Approval, InProgress, Queued, Success, Partial Success, Failed
    isGlobalDeployment: boolean;
    totalTargets: number;
    successCount: number;
    failedCount: number;
    pendingCount: number;
    progressPercentage: number;
    deployedBy: string;
    deployedAt: string;
    completedAt: string | null;
    errorMessage: string | null;
    targetMachines: string[];
    targetUsers: string[];
    requiresApproval: boolean;
    scheduledFor: string | null;
    approvedBy: string;
    approvedAt: string;
}