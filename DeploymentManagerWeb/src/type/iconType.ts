export interface IconResponse {
    id: number;
    name: string;
    type: number;
    fileName: string;
    filePath: string;
    fileSize: number;
    fileUrl: string;
    description: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
    fileExtension: string;
}

export interface CreateIconRequest {
    name: string;
    type: string;
    description?: string;
    file: File;
}

export interface UpdateIconRequest {
    name: string;
    type: string;
    description?: string;
    isActive: boolean;
    file?: File;
}
