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
}

export interface CategoryCreateOrUpdateRequest {
    name: string;
    displayName: string;
    description?: string;
    icon: string;
    displayOrder: number;
}

export interface CategoryResponse {
    id: number;
    name: string;
    displayName: string;
    description?: string;
    icon: string;
    displayOrder: number;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
    applicationCount: number;
}