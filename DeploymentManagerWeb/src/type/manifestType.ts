export interface ManifestCreateRequest {
    Version: string;
    BinaryVersion: string;
    BinaryPackage: string;
    BinaryFiles: string[];
    ConfigVersion: string;
    ConfigPackage: string;
    ConfigMergeStrategy: string;
    ConfigFiles: ConfigFileRequest[];
    UpdateType: string;
    ForceUpdate: boolean;
    //NotifyUser: boolean;
    //AllowSkip: boolean;
    //UpdateDescription: string;
    ReleaseNotes: string;
    IsStable: boolean;
    PublishedAt: string;
}

export interface ConfigFileRequest {
    name: string;
    updatePolicy: string;
    priority: string;
}

export interface ManifestUpdateRequest {
    Version: string;
    BinaryVersion: string;
    BinaryPackage: string;
    BinaryFiles: string[];
    ConfigVersion: string;
    ConfigPackage: string;
    ConfigMergeStrategy: string;
    ConfigFiles: ConfigFileRequest[];
    UpdateType: string;
    ForceUpdate: boolean;
    //NotifyUser: boolean;
    //AllowSkip: boolean;
    //UpdateDescription: string;
    ReleaseNotes: string;
    IsStable: boolean;
    PublishedAt: string;
}

export interface ManifestResponse {
    id: number;
    applicationId: number;
    appCode: string;
    appName: string;
    version: string;
    binaryVersion: string;
    binaryPackage: string;
    binaryFiles: string[];
    configVersion: string;
    configPackage: string;
    configFiles: ConfigFileResponse[];
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

export interface ConfigFileResponse {
    name: string;
    updatePolicy: string;
    priority: string;
}