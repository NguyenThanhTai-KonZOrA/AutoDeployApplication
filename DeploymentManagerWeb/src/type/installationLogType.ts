export interface InstallationLogRequest {
    ApplicationId?: number;
    UserName?: string;
    MachineName?: string;
    Status?: string; // Dropdown list: Success, Failed, InProgress
    Action?: string; // Dropdown list: Install, Update, Uninstall
    FromDate?: string;
    ToDate?: string;
    Page: number;
    PageSize: number;
    Take: number;
    Skip: number;
}

export interface InstallationLogResponse {
    id: number;
    applicationId: number;
    userName: string;
    machineName: string;
    status: string; // Success, Failed, InProgress
    action: string; // Install, Update, Uninstall
    errorMessage: string;
    stackTrace: string;
    oldVersion: string;
    newVersion: string;
    installationPath: string;
    startedAt: string;
    completedAt: string;
    durationInSeconds: number;
}

export interface InstallationLogPaginationResponse {
    totalRecords: number;
    pageSize: number;
    Page: number;
    logs: InstallationLogResponse[];
}

export interface InstallationReportRequest {
    ApplicationId?: number;
    MachineName?: string;
    Status?: string; // Dropdown list: Success, Failed, InProgress
    FromDate?: string;
    ToDate?: string;
}

export interface InstallationReportResponse {
    applicationId: number;
    applicationName: string;
    appCode: string;
    versionStats: VersionInstallationStats[];
    totalPCs: number;
}

export interface VersionInstallationStats {
    version: string;
    pcCount: number;
    notUpdatedPCCount: number;
    updatedPCCount: number;
    pCs: PCInstallationDetail[];
}

export interface PCInstallationDetail {
    machineName: string;
    machineId: number;
    userName: string;
    status: string; // Success, Failed, InProgress
    action: string; // Install, Update, Uninstall
    installedAt: string;
    lastUpdatedAt: string;
}