export interface CategoryCreateOrUpdateRequest {
    name: string;
    displayName: string;
    description?: string;
    icon: string;
    iconUrl: string;
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
    iconUrl: string;
}