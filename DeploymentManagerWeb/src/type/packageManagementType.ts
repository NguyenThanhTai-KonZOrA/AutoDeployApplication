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
    MinimumClientVersion: string;
    IsStable: boolean;
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