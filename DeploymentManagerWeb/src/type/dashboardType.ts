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
    // Chart data for UI
    installationTrends: InstallationTrends[];
    topUpdateApplications: ApplicationUpdateStats[];
    mostActiveApplications: ApplicationActivityStats[];
    monthlyComparison: MonthlyComparisonStats;
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

export interface InstallationTrends {
    appCode: string;
    applicationName: string;
    currentMonthInstallations: number;
    previousMonthInstallations: number;
    growthCount: number;
    growthPercentage: number;
}

export interface ApplicationUpdateStats {
    appCode: string;
    applicationName: string;
    totalUpdates: number;
    updatesThisMonth: number;
    latestVersion: string;
    lastUpdateDate?: string;
}

export interface ApplicationActivityStats {
    appCode: string;
    applicationName: string;
    totalActiveMachines: number;
    todayActiveMachines: number;
    weekActiveMachines: number;
    monthActiveMachines: number;
    lastActivityDate?: string;
}

export interface MonthlyComparisonStats {
    currentMonthInstallations: number;
    previousMonthInstallations: number;
    currentMonthDownloads: number;
    previousMonthDownloads: number;
    currentMonthActiveApps: number;
    previousMonthActiveApps: number;
    installationGrowthPercentage: number;
    downloadGrowthPercentage: number;
}