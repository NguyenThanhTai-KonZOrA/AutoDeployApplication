export interface ManifestCreateRequest {
    Version: string;
    BinaryVersion: string;
    BinaryPackage: string;
    ConfigVersion: string;
    ConfigPackage: string;
    ConfigMergeStrategy: string;
    UpdateType: string;
    ForceUpdate: boolean;
    ReleaseNotes: string;
    IsStable: boolean;
    PublishedAt: string;
}

export interface ManifestUpdateRequest {
    Version?: string;
    BinaryVersion?: string;
    BinaryPackage?: string;
    ConfigVersion?: string;
    ConfigPackage?: string;
    ConfigMergeStrategy?: string;
    UpdateType?: string;
    ForceUpdate?: boolean;
    ReleaseNotes?: string;
    IsStable?: boolean;
    PublishedAt?: string;
}

export interface ManifestResponse {
    id: number;
    applicationId: number;
    appCode: string;
    appName: string;
    version: string;
    binaryVersion: string;
    binaryPackage: string;
    configVersion: string;
    configPackage: string;
    configMergeStrategy: string;
    updateType: string;
    forceUpdate: boolean;
    releaseNotes: string;
    isStable: boolean;
    publishedAt: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}