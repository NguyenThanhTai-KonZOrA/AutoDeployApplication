export interface CreateIconRequest {
    Name: string;
    Type: number;   // Application = 1,  Category = 2
    ReferenceId?: number;
    File: File;
}

export interface UpdateIconRequest {
    Name?: string;
    Type?: number;  // Application = 1,  Category = 2
    ReferenceId?: number;
    File?: File;
}

export interface IconResponse {
    id: number;
    name: string;
    type: number;  // Application = 1,  Category = 2
    fileUrl: string;
    fileExtension: string;
    fileSize: number;
    createdAt: string;
    updatedAt: string;
}