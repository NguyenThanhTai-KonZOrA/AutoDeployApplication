import type { CategoryResponse } from "./categoryType";

export interface AnalyticDashboardResponse {
    totalApplications: number;
    activeApplications: number;
    totalVersions: number;
    totalInstallations: number;
    totalStorageUsed: number;
    totalStorageFormatted: string;
    todayDownloads: number;
    weekDownloads: number;
    monthDownloads: number;
    pendingDeployments: number;
    failedInstallations: number;
    successfulInstallations: number;
    topApplications: TopApplicationResponse[];
    recentActivities: RecentActivitiesResponse[];
    categories: CategoryResponse[];
}

export interface TopApplicationResponse {
    appCode: string;
    applicationName: string;
    downloadCount: number;
    latestVersion: string;
    iconUrl: string;
}

export interface RecentActivitiesResponse {
    type: string;
    applicationName: string;
    version: string;
    user: string;
    timestamp: string;
    status: string;
}