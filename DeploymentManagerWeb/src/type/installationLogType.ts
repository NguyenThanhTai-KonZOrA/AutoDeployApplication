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