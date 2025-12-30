export interface CreateApplicationRequest {
    appCode: string;
    name: string;
    description: string;
    iconUrl: string;
    categoryId: number;
}

export interface UpdateApplicationRequest {
    name?: string;
    description?: string;
    iconUrl?: string;
    categoryId?: number;
}

export interface ApplicationResponse {
    id: number;
    manifestId: number;
    appCode: string;
    name: string;
    description: string;
    iconUrl: string;
    categoryId: number;
    categoryName: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
    latestVersion: string;
    latestVersionDate: string;
    totalVersions: number;
    totalInstalls: number;
    totalStorageSize: number;
    //package information
    packageId: number;
    packageFileName: string;
    packageType: string;
    packageVersion: string;
    isStable: boolean;
    releaseNotes: string;
    minimumClientVersion: string;
}