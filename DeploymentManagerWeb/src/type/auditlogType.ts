export interface AuditLogPaginationRequest {
  Page: number;
  PageSize: number;
  Take?: number;
  Skip?: number;
  FromDate?: string;
  ToDate?: string;
  Action?: string;
  UserName?: string;
  IsSuccess?: boolean;
  EntityType?: string;
}

export interface AuditLogPaginationResponse {
  totalUsedApplications: number;
  page: number;
  pageSize: number;
  totalRecords: number;
  logs: AuditLogResponse[];
}

export interface AuditLogResponse {
  id: number;
  userName: string;
  action: string;
  entityType: string;
  httpMethod: string;
  requestPath: string;
  ipAddress: string;
  userAgent: string;
  isSuccess: boolean;
  statusCode: number;
  errorMessage: string;
  details: string;
  createdAt: string;
  entityId: string;
  timestamp: string;
}