export interface PackageUploadRequest {
    ApplicationId: number;
    Version: string;
    PackageType: string;
    PackageFile: File;
    ReleaseNotes: string;
    IsStable: boolean;
    MinimumClientVersion: string;
    UploadedBy: string;
    PublishImmediately: boolean;
}

export interface PackageUpdateRequest {
    ReleaseNotes: string;
    IsActive: boolean;
    MinimumClientVersion?: string;
    IsStable: boolean;
    NewPackage: File;
}

export interface PackageVersionResponse {
    id: number;
    version: string;
    packageType: string;
    releaseNotes: string;
    isStable: boolean;
    minimumClientVersion: string;
    uploadedBy: string;
    publishImmediately: boolean;
}

export interface ApplicationPackageResponse {
    id: number;
    applicationId: number;
    applicationName: string;
    version: string;
    packageFileName: string;
    packageType: string;
    fileSizeBytes: number;
    fileSizeFormatted: string;
    storagePath: string;
    downloadCount: number;
    lastDownloadedAt: string;
    replacesVersionId: number;
    replacesVersionNumber: string;
}

export interface ApplicationPackageHistoryResponse {
    [appCode: string]: ApplicationPackageResponse[];
}
