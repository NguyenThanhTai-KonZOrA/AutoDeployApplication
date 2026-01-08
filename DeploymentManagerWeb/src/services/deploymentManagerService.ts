import { showSessionExpiredNotification } from "../utils/showSessionExpiredNotification";
import type { ApiEnvelope } from "../type/commonType";
import axios from "axios";
import type { ApplicationResponse, CreateApplicationRequest, UpdateApplicationRequest } from "../type/applicationType";
import type { ManifestCreateRequest, ManifestResponse, ManifestUpdateRequest } from "../type/manifestType";
import type { CategoryCreateOrUpdateRequest, CategoryResponse } from "../type/categoryType";
import type { InstallationLogRequest, InstallationLogPaginationResponse, InstallationReportResponse, InstallationReportRequest } from "../type/installationLogType";
import type { ApplicationPackageHistoryResponse, ApplicationPackageResponse, PackageUploadRequest, PackageVersionResponse } from "../type/packageManagementType";
import type { AnalyticDashboardResponse } from "../type/dashboardType";
import type { CreateIconRequest, IconResponse, UpdateIconRequest } from "../type/iconType";

const API_BASE = (window as any)._env_?.API_BASE;
const api = axios.create({
    baseURL: API_BASE,
    headers: { "Content-Type": "application/json" }
});

// Add token to requests automatically
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Handle 401 responses - redirect to login
api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            // Only redirect if this is not a login request or token validation
            const isLoginRequest = error.config?.url?.includes('/api/auth/login');
            const isTokenValidation = error.config?.headers?.['X-Token-Validation'] === 'true';

            if (!isLoginRequest && !isTokenValidation) {
                console.log('🔒 Received 401 Unauthorized - Token is invalid or expired');

                // Clear all auth data
                localStorage.removeItem('token');
                localStorage.removeItem('user');
                localStorage.removeItem('userRoles');

                // Trigger logout event for other tabs
                localStorage.setItem('logout-event', Date.now().toString());

                // Show user-friendly message
                showSessionExpiredNotification();
                console.log('🚪 Redirecting to login page...');

                // Redirect to login
                // Delay redirect to show notification
                setTimeout(() => {
                    window.location.href = '/login';
                }, 5000);
            }
        }

        // Check if it's a network error
        if (!error.response && error.code === 'ERR_NETWORK') {
            console.error('Network error detected:', error.message);
            // Dispatch event to update network status
            window.dispatchEvent(new CustomEvent('network-error', { detail: error }));
        }

        return Promise.reject(error);
    }
);

function unwrapApiEnvelope<T>(response: { data: ApiEnvelope<T> }): T {
    if (!response.data.success) {
        throw new Error("API call failed");
    }
    return response.data.data;
}

export const applicationService = {
    createApplication: async (request: CreateApplicationRequest): Promise<ApplicationResponse> => {
        const response = await api.post('/api/ApplicationManagement/create', request);
        return unwrapApiEnvelope(response);
    },

    updateApplication: async (id: number, request: UpdateApplicationRequest): Promise<ApplicationResponse> => {
        const response = await api.post(`/api/ApplicationManagement/update/${id}`, request);
        return unwrapApiEnvelope(response);
    },

    deleteApplication: async (id: number): Promise<void> => {
        const response = await api.post(`/api/ApplicationManagement/delete/${id}`);
        return unwrapApiEnvelope(response);
    },

    changeStatusApplication: async (id: number): Promise<boolean> => {
        const response = await api.post(`/api/ApplicationManagement/change-status/${id}`);
        return unwrapApiEnvelope(response);
    },

    getApplicationById: async (id: number): Promise<ApplicationResponse> => {
        const response = await api.get(`/api/ApplicationManagement/${id}`);
        return unwrapApiEnvelope(response);
    },

    getAllApplications: async (): Promise<ApplicationResponse[]> => {
        const response = await api.get(`/api/ApplicationManagement/all`);
        return unwrapApiEnvelope(response);
    },

    getApplicationManifest: async (id: number): Promise<any> => {
        const response = await api.get(`/api/ApplicationManagement/${id}/manifest/latest`);
        return unwrapApiEnvelope(response);
    },

    createApplicationManifest: async (id: number, manifest: ManifestCreateRequest): Promise<ManifestResponse> => {
        const response = await api.post(`/api/ApplicationManagement/${id}/manifest`, manifest);
        return unwrapApiEnvelope(response);
    },

    updateApplicationManifest: async (id: number, manifestId: number, manifest: ManifestUpdateRequest): Promise<ManifestResponse> => {
        const response = await api.post(`/api/ApplicationManagement/update/${id}/manifest/${manifestId}`, manifest);
        return unwrapApiEnvelope(response);
    },

    deleteApplicationManifest: async (id: number): Promise<void> => {
        const response = await api.post(`/api/ApplicationManagement/${id}/manifest`);
        return unwrapApiEnvelope(response);
    }
};

export const categoryService = {
    getAllCategories: async (): Promise<CategoryResponse[]> => {
        const response = await api.get(`/api/Category/all`);
        return unwrapApiEnvelope(response);
    },

    createCategory: async (request: CategoryCreateOrUpdateRequest): Promise<CategoryResponse> => {
        const response = await api.post(`/api/Category/create`, request);
        return unwrapApiEnvelope(response);
    },

    updateCategory: async (id: number, request: CategoryCreateOrUpdateRequest): Promise<CategoryResponse> => {
        const response = await api.post(`/api/Category/update/${id}`, request);
        return unwrapApiEnvelope(response);
    },

    deleteCategory: async (id: number): Promise<void> => {
        const response = await api.post(`/api/Category/delete/${id}`);
        return unwrapApiEnvelope(response);
    },

    getCategoryDetail: async (id: number): Promise<CategoryResponse> => {
        const response = await api.get(`/api/Category/${id}`);
        return unwrapApiEnvelope(response);
    }
};

export const installationService = {
    getInstallationLogs: async (request: InstallationLogRequest): Promise<InstallationLogPaginationResponse> => {
        const response = await api.post(`/api/InstallationLog/logs`, request);
        return unwrapApiEnvelope(response);
    },

    getReportByApplication: async (request: InstallationReportRequest): Promise<InstallationReportResponse[]> => {
        const response = await api.post(`/api/InstallationLog/report/by-version`, request);
        return unwrapApiEnvelope(response);
    }
};

export const packageManagementService = {
    uploadPackage: async (formData: FormData): Promise<PackageVersionResponse> => {
        const response = await api.post(`/api/PackageManagement/upload`, formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return unwrapApiEnvelope(response);
    },

    downloadPackage: async (id: number, packageName: string): Promise<void> => {
        try {
            const response = await api.get(`/api/PackageManagement/${id}/download`, {
                responseType: 'blob'
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement("a");
            link.href = url;
            link.setAttribute("download", `${packageName}.zip`);
            document.body.appendChild(link);
            link.click();
        } catch (error) {
            console.error("Error downloading package:", error);
            throw error;
        }
    },

    getPackageDetails: async (id: number): Promise<PackageVersionResponse> => {
        const response = await api.get(`/api/PackageManagement/${id}`);
        return unwrapApiEnvelope(response);
    },

    getPackageByApplicationId: async (applicationId: number): Promise<ApplicationPackageResponse[]> => {
        const response = await api.get(`/api/PackageManagement/application/${applicationId}/history`);
        return unwrapApiEnvelope(response);
    },

    getPackageHistoryByApplication: async (): Promise<ApplicationPackageHistoryResponse> => {
        const response = await api.get(`/api/PackageManagement/all-by-applications`);
        return unwrapApiEnvelope(response);
    },

    deletePackage: async (id: number): Promise<boolean> => {
        const response = await api.post(`/api/PackageManagement/delete/${id}`);
        return unwrapApiEnvelope(response);
    },

    updatePackage: async (formData: FormData): Promise<PackageVersionResponse> => {
        const response = await api.post(`/api/PackageManagement/update`, formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return unwrapApiEnvelope(response);
    }
};

export const dashboardService = {
    getDashboardData: async (): Promise<AnalyticDashboardResponse> => {
        const response = await api.get(`/api/Analytics/Dashboard`);
        return unwrapApiEnvelope(response);
    }
};

export const iconService = {
    createIcon: async (formData: FormData): Promise<IconResponse> => {
        const response = await api.post(`/api/Icons/create`, formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return unwrapApiEnvelope(response);
    },

    updateIcon: async (id: number, formData: FormData): Promise<IconResponse> => {
        const response = await api.post(`/api/Icons/update/${id}`, formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
        return unwrapApiEnvelope(response);
    },

    deleteIcon: async (id: number): Promise<void> => {
        const response = await api.post(`/api/Icons/delete/${id}`);
        return unwrapApiEnvelope(response);
    },

    getIconDetail: async (id: number): Promise<IconResponse> => {
        const response = await api.get(`/api/Icons/${id}`);
        return unwrapApiEnvelope(response);
    },

    getIconByType: async (type: number): Promise<IconResponse[]> => {
        const response = await api.get(`/api/Icons/type/${type}`);
        return unwrapApiEnvelope(response);
    },

    getAllIcons: async (): Promise<IconResponse[]> => {
        const response = await api.get(`/api/Icons/all`);
        return unwrapApiEnvelope(response);
    }
};