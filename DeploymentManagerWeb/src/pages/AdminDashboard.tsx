import {
    Box,
    Card,
    CardContent,
    Typography,
    LinearProgress,
    Alert,
    Snackbar,
    IconButton,
    Tooltip,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Chip,
    Avatar,
    List,
    ListItem,
    ListItemAvatar,
    ListItemText,
    Divider,
} from "@mui/material";
import {
    Refresh as RefreshIcon,
    Apps as AppsIcon,
    CheckCircle as CheckCircleIcon,
    Inventory as InventoryIcon,
    Download as DownloadIcon,
    Storage as StorageIcon,
    Warning as WarningIcon,
    Error as ErrorIcon,
    TrendingUp as TrendingUpIcon,
    Update as UpdateIcon,
    InstallMobile as InstallMobileIcon,
    Category as CategoryIcon,
    InstallDesktop as InstallDesktopIcon,
    DoneAll as DoneAllIcon,
    TrendingDown as TrendingDownIcon,
    ShowChart as ShowChartIcon,
    HotelClass as HotelClassIcon,
    BrowserUpdated as BrowserUpdatedIcon,
} from "@mui/icons-material";
import { useState, useEffect } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { dashboardService } from "../services/deploymentManagerService";
import type { AnalyticDashboardResponse } from "../type/dashboardType";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { LineChart, Line, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, Legend, ResponsiveContainer } from 'recharts';

interface StatCardProps {
    title: string;
    value: string | number;
    icon: React.ReactNode;
    color: string;
    subtitle?: string;
}

const StatCard = ({ title, value, icon, color, subtitle }: StatCardProps) => (
    <Card sx={{ height: "100%", position: "relative", overflow: "visible" }}>
        <CardContent>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                <Box>
                    <Typography color="text.secondary" variant="body2" gutterBottom>
                        {title}
                    </Typography>
                    <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
                        {value}
                    </Typography>
                    {subtitle && (
                        <Typography variant="caption" color="text.secondary">
                            {subtitle}
                        </Typography>
                    )}
                </Box>
                <Avatar
                    sx={{
                        bgcolor: color,
                        width: 56,
                        height: 56,
                        boxShadow: 2,
                    }}
                >
                    {icon}
                </Avatar>
            </Box>
        </CardContent>
    </Card>
);

export default function AdminDashboard() {
    useSetPageTitle(PAGE_TITLES.DASHBOARD);
    const API_BASE = (window as any)._env_?.API_BASE;
    const [loading, setLoading] = useState<boolean>(false);
    const [dashboardData, setDashboardData] = useState<AnalyticDashboardResponse | null>(null);
    const [snackbar, setSnackbar] = useState<{
        open: boolean;
        message: string;
        severity: "success" | "error" | "info";
    }>({ open: false, message: "", severity: "info" });

    const loadDashboardData = async () => {
        setLoading(true);
        try {
            const data = await dashboardService.getDashboardData();
            setDashboardData(data);
        } catch (error: any) {
            console.error("Error loading dashboard data:", error);
            setSnackbar({
                open: true,
                message: error?.response?.data?.message || "Failed to load dashboard data",
                severity: "error",
            });
        } finally {
            setLoading(false);
        }
    };

    const getActivityIcon = (type: string) => {
        switch (type.toLowerCase()) {
            case "upload":
            case "created":
                return <UpdateIcon fontSize="small" color="primary" />;
            case "download":
                return <DownloadIcon fontSize="small" color="info" />;
            case "install":
            case "installation":
                return <InstallDesktopIcon fontSize="small" color="success" />;
            case "failed":
            case "error":
                return <ErrorIcon fontSize="small" color="error" />;
            default:
                return <CheckCircleIcon fontSize="small" color="action" />;
        }
    };

    const getStatusColor = (status: string): "success" | "error" | "warning" | "default" => {
        switch (status.toLowerCase()) {
            case "success":
            case "completed":
                return "success";
            case "failed":
            case "error":
                return "error";
            case "pending":
            case "processing":
                return "warning";
            default:
                return "default";
        }
    };

    useEffect(() => {
        loadDashboardData();
    }, []);

    if (!dashboardData && !loading) {
        return (
            <AdminLayout>
                <Box sx={{ p: 3 }}>
                    <Alert severity="info">No dashboard data available</Alert>
                </Box>
            </AdminLayout>
        );
    }

    return (
        <AdminLayout>
            <Box sx={{ p: 3 }}>
                <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3 }}>
                    <Box>
                        {/* <Typography variant="h4" fontWeight={700} gutterBottom>
                            Dashboard
                        </Typography> */}
                        <Typography variant="body2" color="text.secondary">
                            Overview of your deployment management system
                        </Typography>
                    </Box>
                    <Tooltip title="Refresh">
                        <IconButton onClick={loadDashboardData} disabled={loading} color="primary">
                            <RefreshIcon />
                        </IconButton>
                    </Tooltip>
                </Box>

                {loading && <LinearProgress sx={{ mb: 3 }} />}

                {dashboardData && (
                    <>
                        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", md: "repeat(4, 1fr)" }, gap: 3, mb: 3 }}>
                            <StatCard
                                title="Total Applications"
                                value={dashboardData.totalApplications}
                                icon={<AppsIcon />}
                                color="primary.main"
                                subtitle={`${dashboardData.activeApplications} active`}
                            />
                            <StatCard
                                title="Total Versions"
                                value={dashboardData.totalVersions}
                                icon={<InventoryIcon />}
                                color="success.main"
                                subtitle="Package versions"
                            />
                            <StatCard
                                title="Storage Used"
                                value={dashboardData.totalStorageFormatted}
                                icon={<StorageIcon />}
                                color="warning.main"
                                subtitle={`${dashboardData.totalStorageUsed.toLocaleString()} bytes`}
                            />
                            <StatCard
                                title="Total Installations"
                                value={dashboardData.totalInstallations}
                                icon={<InstallDesktopIcon />}
                                color="info.main"
                                subtitle="All time"
                            />
                        </Box>

                        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" }, gap: 3, mb: 3 }}>
                            <Card>
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                        <DownloadIcon color="primary" sx={{ mr: 1 }} />
                                        <Typography variant="h6" fontWeight={600}>
                                            Downloads
                                        </Typography>
                                    </Box>
                                    <Box sx={{ mb: 2 }}>
                                        <Box sx={{ display: "flex", justifyContent: "space-between", mb: 1 }}>
                                            <Typography variant="body2" color="text.secondary">Today</Typography>
                                            <Typography variant="h6" fontWeight={600}>{dashboardData.todayDownloads}</Typography>
                                        </Box>
                                        <Box sx={{ display: "flex", justifyContent: "space-between", mb: 1 }}>
                                            <Typography variant="body2" color="text.secondary">Last Week</Typography>
                                            <Typography variant="h6" fontWeight={600}>{dashboardData.weekDownloads}</Typography>
                                        </Box>
                                        <Box sx={{ display: "flex", justifyContent: "space-between" }}>
                                            <Typography variant="body2" color="text.secondary">This Month</Typography>
                                            <Typography variant="h6" fontWeight={600}>{dashboardData.monthDownloads}</Typography>
                                        </Box>
                                    </Box>
                                </CardContent>
                            </Card>

                            <Card>
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                        <DoneAllIcon color="success" sx={{ mr: 1 }} />
                                        <Typography variant="h6" fontWeight={600}>Successful Installations</Typography>
                                    </Box>
                                    <Typography variant="h3" fontWeight={600} color="success.main">
                                        {dashboardData.successfulInstallations}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">Count installed successfully</Typography>
                                </CardContent>
                            </Card>

                            <Card>
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                        <ErrorIcon color="error" sx={{ mr: 1 }} />
                                        <Typography variant="h6" fontWeight={600}>Failed Installations</Typography>
                                    </Box>
                                    <Typography variant="h3" fontWeight={600} color="error.main">
                                        {dashboardData.failedInstallations}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">Require attention</Typography>
                                </CardContent>
                            </Card>
                        </Box>

                        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", lg: "2fr 1fr" }, gap: 3, mb: 3 }}>
                            <Card>
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                        <HotelClassIcon sx={{ mr: 1, verticalAlign: 'middle', color: 'gold' }} />
                                        <Typography variant="h6" fontWeight={600}>Top Applications</Typography>
                                    </Box>
                                    <TableContainer>
                                        <Table size="small">
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell>#</TableCell>
                                                    <TableCell>Icon</TableCell>
                                                    <TableCell>Application Name</TableCell>
                                                    <TableCell>Application Code</TableCell>
                                                    <TableCell>Latest Version</TableCell>
                                                    <TableCell align="right">Downloads</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {dashboardData.topApplications.length === 0 ? (
                                                    <TableRow>
                                                        <TableCell colSpan={5} align="center">
                                                            <Typography variant="body2" color="text.secondary">
                                                                No application data available
                                                            </Typography>
                                                        </TableCell>
                                                    </TableRow>
                                                ) : (
                                                    dashboardData.topApplications.map((app, index) => (
                                                        <TableRow key={app.appCode} hover>
                                                            <TableCell>
                                                                <Chip
                                                                    label={index + 1}
                                                                    size="small"
                                                                    color={index === 0 ? "primary" : index === 1 ? "success" : index === 2 ? "warning" : "default"}
                                                                />
                                                            </TableCell>
                                                            <TableCell>
                                                                <Box
                                                                    component="img"
                                                                    src={`${API_BASE}${app.iconUrl}`}
                                                                    alt={app.applicationName}
                                                                    sx={{
                                                                        width: 40,
                                                                        height: 40,
                                                                        objectFit: 'contain',
                                                                        borderRadius: 1,
                                                                        p: 0.5,
                                                                    }}
                                                                    onError={(e) => {
                                                                        (e.target as HTMLImageElement).style.display = 'none';
                                                                    }}
                                                                />
                                                            </TableCell>
                                                            <TableCell>
                                                                <Typography variant="body2" fontWeight={600}>
                                                                    {app.applicationName}
                                                                </Typography>
                                                            </TableCell>
                                                            <TableCell>
                                                                <Chip label={app.appCode} size="small" variant="outlined" />
                                                            </TableCell>
                                                            <TableCell>
                                                                <Chip label={app.latestVersion} size="small" color="info" />
                                                            </TableCell>
                                                            <TableCell align="right">
                                                                <Typography variant="body2" fontWeight={600}>
                                                                    {app.downloadCount.toLocaleString()}
                                                                </Typography>
                                                            </TableCell>
                                                        </TableRow>
                                                    ))
                                                )}
                                            </TableBody>
                                        </Table>
                                    </TableContainer>
                                </CardContent>
                            </Card>

                            <Card sx={{ height: "100%" }}>
                                <CardContent>
                                    <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                        <CategoryIcon color="primary" sx={{ mr: 1 }} />
                                        <Typography variant="h6" fontWeight={600}>Categories</Typography>
                                    </Box>
                                    <List dense>
                                        {dashboardData.categories.length === 0 ? (
                                            <Typography variant="body2" color="text.secondary" align="center">
                                                No categories available
                                            </Typography>
                                        ) : (
                                            dashboardData.categories.map((category, index) => (
                                                <Box key={category.id}>
                                                    <ListItem>
                                                        <ListItemAvatar>
                                                            <Avatar>
                                                                <Box
                                                                    component="img"
                                                                    src={`${API_BASE}${category.iconUrl}`}
                                                                    alt={category.name}
                                                                    sx={{
                                                                        width: 40,
                                                                        height: 40,
                                                                        objectFit: 'contain',
                                                                        borderRadius: 1,
                                                                        p: 0.5,
                                                                    }}
                                                                    onError={(e) => {
                                                                        (e.target as HTMLImageElement).style.display = 'none';
                                                                    }}
                                                                />
                                                            </Avatar>
                                                        </ListItemAvatar>
                                                        <ListItemText
                                                            primary={category.name}
                                                            secondary={category.description}
                                                            primaryTypographyProps={{ fontWeight: 600 }}
                                                        />
                                                    </ListItem>
                                                    {index < dashboardData.categories.length - 1 && <Divider variant="inset" component="li" />}
                                                </Box>
                                            ))
                                        )}
                                    </List>
                                </CardContent>
                            </Card>
                        </Box>

                        {/* Installation Trends Line Chart */}
                        {dashboardData.installationTrends && dashboardData.installationTrends.length > 0 && (
                            <>
                                <Card sx={{ mb: 3 }}>
                                    <CardContent>
                                        <Box sx={{ mb: 2 }}>
                                            <Typography variant="h5" fontWeight={700} gutterBottom>
                                                <TrendingUpIcon color="primary" sx={{ mr: 1, verticalAlign: 'middle', color: '#1976d2' }} />
                                                Installation Trends
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                Compare current month vs previous month installations by application
                                            </Typography>
                                        </Box>
                                        <Box sx={{ width: "100%", height: 400 }}>
                                            <ResponsiveContainer>
                                                <LineChart
                                                    data={dashboardData.installationTrends}
                                                    margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
                                                >
                                                    <CartesianGrid strokeDasharray="3 3" />
                                                    <XAxis
                                                        dataKey="applicationName"
                                                        angle={-45}
                                                        textAnchor="end"
                                                        height={100}
                                                    />
                                                    <YAxis />
                                                    <RechartsTooltip />
                                                    <Legend />
                                                    <Line
                                                        type="monotone"
                                                        dataKey="currentMonthInstallations"
                                                        stroke="#1976d2"
                                                        strokeWidth={2}
                                                        name="Current Month"
                                                        activeDot={{ r: 8 }}
                                                    />
                                                    <Line
                                                        type="monotone"
                                                        dataKey="previousMonthInstallations"
                                                        stroke="#82ca9d"
                                                        strokeWidth={2}
                                                        name="Previous Month"
                                                        activeDot={{ r: 8 }}
                                                    />
                                                </LineChart>
                                            </ResponsiveContainer>
                                        </Box>

                                        {/* Growth details table */}
                                        <Box sx={{ mt: 3 }}>
                                            <Typography variant="h6" fontWeight={600} gutterBottom>
                                                Growth Details
                                            </Typography>
                                            <TableContainer>
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 600 }}>Application</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Current</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Previous</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Growth</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>%</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {dashboardData.installationTrends.map((trend) => (
                                                            <TableRow key={trend.appCode} hover>
                                                                <TableCell>
                                                                    <Typography variant="body2" fontWeight={600}>
                                                                        {trend.applicationName}
                                                                    </Typography>
                                                                    <Typography variant="caption" color="text.secondary">
                                                                        {trend.appCode}
                                                                    </Typography>
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={trend.currentMonthInstallations} size="small" color="primary" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={trend.previousMonthInstallations} size="small" variant="outlined" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "flex-end" }}>
                                                                        {trend.growthCount >= 0 ? (
                                                                            <TrendingUpIcon color="success" fontSize="small" sx={{ mr: 0.5 }} />
                                                                        ) : (
                                                                            <TrendingDownIcon color="error" fontSize="small" sx={{ mr: 0.5 }} />
                                                                        )}
                                                                        <Typography
                                                                            variant="body2"
                                                                            fontWeight={600}
                                                                            color={trend.growthCount >= 0 ? "success.main" : "error.main"}
                                                                        >
                                                                            {trend.growthCount > 0 ? '+' : ''}{trend.growthCount}
                                                                        </Typography>
                                                                    </Box>
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip
                                                                        label={`${trend.growthPercentage > 0 ? '+' : ''}${trend.growthPercentage.toFixed(1)}%`}
                                                                        size="small"
                                                                        color={trend.growthPercentage >= 0 ? "success" : "error"}
                                                                    />
                                                                </TableCell>
                                                            </TableRow>
                                                        ))}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </>
                        )}

                        {/* Top Update Applications Bar Chart */}
                        {dashboardData.topUpdateApplications && dashboardData.topUpdateApplications.length > 0 && (
                            <>
                                <Card sx={{ mb: 3 }}>
                                    <CardContent>
                                        <Box sx={{ mb: 2 }}>
                                            <Typography variant="h5" fontWeight={700} gutterBottom>
                                                <BrowserUpdatedIcon sx={{ mr: 1, verticalAlign: "middle", color: 'warning.main' }} />
                                                Top Update Applications
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                Applications with most updates and versions
                                            </Typography>
                                        </Box>
                                        <Box sx={{ width: "100%", height: 400 }}>
                                            <ResponsiveContainer>
                                                <BarChart
                                                    data={dashboardData.topUpdateApplications}
                                                    margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
                                                >
                                                    <CartesianGrid strokeDasharray="3 3" />
                                                    <XAxis
                                                        dataKey="applicationName"
                                                        angle={-45}
                                                        textAnchor="end"
                                                        height={100}
                                                    />
                                                    <YAxis />
                                                    <RechartsTooltip />
                                                    <Legend />
                                                    <Bar
                                                        dataKey="totalUpdates"
                                                        fill="#1976d2"
                                                        name="Total Updates"
                                                        radius={[8, 8, 0, 0]}
                                                    />
                                                    <Bar
                                                        dataKey="updatesThisMonth"
                                                        fill="#82ca9d"
                                                        name="Updates This Month"
                                                        radius={[8, 8, 0, 0]}
                                                    />
                                                </BarChart>
                                            </ResponsiveContainer>
                                        </Box>

                                        {/* Updates details table */}
                                        <Box sx={{ mt: 3 }}>
                                            <Typography variant="h6" fontWeight={600} gutterBottom>
                                                Update Details
                                            </Typography>
                                            <TableContainer>
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 600 }}>Application</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Total Updates</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>This Month</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Latest Version</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Last Update</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {dashboardData.topUpdateApplications.map((app) => (
                                                            <TableRow key={app.appCode} hover>
                                                                <TableCell>
                                                                    <Typography variant="body2" fontWeight={600}>
                                                                        {app.applicationName}
                                                                    </Typography>
                                                                    <Typography variant="caption" color="text.secondary">
                                                                        {app.appCode}
                                                                    </Typography>
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.totalUpdates} size="small" color="primary" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.updatesThisMonth} size="small" color="success" />
                                                                </TableCell>
                                                                <TableCell>
                                                                    <Chip label={app.latestVersion} size="small" color="info" variant="outlined" />
                                                                </TableCell>
                                                                <TableCell>
                                                                    <Typography variant="caption">
                                                                        {app.lastUpdateDate ? FormatUtcTime.formatDateTime(app.lastUpdateDate) : 'Not updated yet'}
                                                                    </Typography>
                                                                </TableCell>
                                                            </TableRow>
                                                        ))}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </>
                        )}

                        {/* Monthly Comparison Stats */}
                        {dashboardData.monthlyComparison && (
                            <>
                                <Box sx={{ mt: 4, mb: 2 }}>
                                    <Typography variant="h5" fontWeight={700} gutterBottom>
                                        Monthly Comparison
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Compare current month vs previous month performance
                                    </Typography>
                                </Box>

                                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" }, gap: 3, mb: 3 }}>
                                    <Card>
                                        <CardContent>
                                            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                                <Box sx={{ flex: 1 }}>
                                                    <Typography color="text.secondary" variant="body2" gutterBottom>
                                                        Installations Growth
                                                    </Typography>
                                                    <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
                                                        {dashboardData.monthlyComparison.currentMonthInstallations}
                                                    </Typography>
                                                    <Typography variant="caption" color="text.secondary">
                                                        Previous: {dashboardData.monthlyComparison.previousMonthInstallations}
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", mt: 1 }}>
                                                        {dashboardData.monthlyComparison.installationGrowthPercentage >= 0 ? (
                                                            <TrendingUpIcon color="success" fontSize="small" />
                                                        ) : (
                                                            <TrendingDownIcon color="error" fontSize="small" />
                                                        )}
                                                        <Typography
                                                            variant="body2"
                                                            fontWeight={600}
                                                            color={dashboardData.monthlyComparison.installationGrowthPercentage >= 0 ? "success.main" : "error.main"}
                                                            sx={{ ml: 0.5 }}
                                                        >
                                                            {Math.abs(dashboardData.monthlyComparison.installationGrowthPercentage).toFixed(1)}%
                                                        </Typography>
                                                    </Box>
                                                </Box>
                                                <Avatar
                                                    sx={{
                                                        bgcolor: "primary.main",
                                                        width: 56,
                                                        height: 56,
                                                        boxShadow: 2,
                                                    }}
                                                >
                                                    <InstallDesktopIcon />
                                                </Avatar>
                                            </Box>
                                        </CardContent>
                                    </Card>

                                    <Card>
                                        <CardContent>
                                            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                                <Box sx={{ flex: 1 }}>
                                                    <Typography color="text.secondary" variant="body2" gutterBottom>
                                                        Downloads Growth
                                                    </Typography>
                                                    <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
                                                        {dashboardData.monthlyComparison.currentMonthDownloads}
                                                    </Typography>
                                                    <Typography variant="caption" color="text.secondary">
                                                        Previous: {dashboardData.monthlyComparison.previousMonthDownloads}
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", mt: 1 }}>
                                                        {dashboardData.monthlyComparison.downloadGrowthPercentage >= 0 ? (
                                                            <TrendingUpIcon color="success" fontSize="small" />
                                                        ) : (
                                                            <TrendingDownIcon color="error" fontSize="small" />
                                                        )}
                                                        <Typography
                                                            variant="body2"
                                                            fontWeight={600}
                                                            color={dashboardData.monthlyComparison.downloadGrowthPercentage >= 0 ? "success.main" : "error.main"}
                                                            sx={{ ml: 0.5 }}
                                                        >
                                                            {Math.abs(dashboardData.monthlyComparison.downloadGrowthPercentage).toFixed(1)}%
                                                        </Typography>
                                                    </Box>
                                                </Box>
                                                <Avatar
                                                    sx={{
                                                        bgcolor: "success.main",
                                                        width: 56,
                                                        height: 56,
                                                        boxShadow: 2,
                                                    }}
                                                >
                                                    <DownloadIcon />
                                                </Avatar>
                                            </Box>
                                        </CardContent>
                                    </Card>

                                    <Card>
                                        <CardContent>
                                            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                                <Box sx={{ flex: 1 }}>
                                                    <Typography color="text.secondary" variant="body2" gutterBottom>
                                                        Active Apps
                                                    </Typography>
                                                    <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
                                                        {dashboardData.monthlyComparison.currentMonthActiveApps}
                                                    </Typography>
                                                    <Typography variant="caption" color="text.secondary">
                                                        Previous: {dashboardData.monthlyComparison.previousMonthActiveApps}
                                                    </Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", mt: 1 }}>
                                                        {dashboardData.monthlyComparison.currentMonthActiveApps >= dashboardData.monthlyComparison.previousMonthActiveApps ? (
                                                            <TrendingUpIcon color="success" fontSize="small" />
                                                        ) : (
                                                            <TrendingDownIcon color="error" fontSize="small" />
                                                        )}
                                                        <Typography
                                                            variant="body2"
                                                            fontWeight={600}
                                                            color={dashboardData.monthlyComparison.currentMonthActiveApps >= dashboardData.monthlyComparison.previousMonthActiveApps ? "success.main" : "error.main"}
                                                            sx={{ ml: 0.5 }}
                                                        >
                                                            {dashboardData.monthlyComparison.currentMonthActiveApps - dashboardData.monthlyComparison.previousMonthActiveApps > 0 ? '+' : ''}
                                                            {dashboardData.monthlyComparison.currentMonthActiveApps - dashboardData.monthlyComparison.previousMonthActiveApps}
                                                        </Typography>
                                                    </Box>
                                                </Box>
                                                <Avatar
                                                    sx={{
                                                        bgcolor: "info.main",
                                                        width: 56,
                                                        height: 56,
                                                        boxShadow: 2,
                                                    }}
                                                >
                                                    <AppsIcon />
                                                </Avatar>
                                            </Box>
                                        </CardContent>
                                    </Card>
                                </Box>
                            </>
                        )}

                        {/* Recent Activities Table */}
                        <Card>
                            <CardContent>
                                <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                    <UpdateIcon color="primary" sx={{ mr: 1, color: 'success.main' }} />
                                    <Typography variant="h6" fontWeight={600}>Recent Activities</Typography>
                                </Box>
                                <TableContainer>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>Type</TableCell>
                                                <TableCell>Application</TableCell>
                                                <TableCell>Version</TableCell>
                                                <TableCell>User</TableCell>
                                                <TableCell>Timestamp</TableCell>
                                                <TableCell>Status</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {dashboardData.recentActivities.length === 0 ? (
                                                <TableRow>
                                                    <TableCell colSpan={6} align="center">
                                                        <Typography variant="body2" color="text.secondary">
                                                            No recent activities
                                                        </Typography>
                                                    </TableCell>
                                                </TableRow>
                                            ) : (
                                                dashboardData.recentActivities.map((activity, index) => (
                                                    <TableRow key={index} hover>
                                                        <TableCell>
                                                            <Box sx={{ display: "flex", alignItems: "center" }}>
                                                                {getActivityIcon(activity.type)}
                                                                <Typography variant="body2" sx={{ ml: 1 }}>
                                                                    {activity.type}
                                                                </Typography>
                                                            </Box>
                                                        </TableCell>
                                                        <TableCell>
                                                            <Typography variant="body2" fontWeight={600}>
                                                                {activity.applicationName}
                                                            </Typography>
                                                        </TableCell>
                                                        <TableCell>
                                                            <Chip label={activity.version ? activity.version : "Not available"} size="small" variant="outlined" />
                                                        </TableCell>
                                                        <TableCell>{activity.user}</TableCell>
                                                        <TableCell>
                                                            {FormatUtcTime.formatDateTime(activity.timestamp)}
                                                        </TableCell>
                                                        <TableCell>
                                                            <Chip
                                                                label={activity.status}
                                                                size="small"
                                                                color={getStatusColor(activity.status)}
                                                            />
                                                        </TableCell>
                                                    </TableRow>
                                                ))
                                            )}
                                        </TableBody>
                                    </Table>
                                </TableContainer>
                            </CardContent>
                        </Card>

                        {/* Most Active Applications Pie Chart */}
                        {dashboardData.mostActiveApplications && dashboardData.mostActiveApplications.length < 0 && (
                            <>
                                <Box sx={{ mt: 4, mb: 2 }}>
                                    <Typography variant="h5" fontWeight={700} gutterBottom>
                                        Most Active Applications
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Applications with the highest number of active machines
                                    </Typography>
                                </Box>

                                <Card sx={{ mb: 3 }}>
                                    <CardContent>
                                        <Box sx={{ width: "100%", height: 400, display: "flex", justifyContent: "center" }}>
                                            <ResponsiveContainer>
                                                <PieChart>
                                                    <Pie
                                                        data={dashboardData.mostActiveApplications}
                                                        cx="50%"
                                                        cy="50%"
                                                        labelLine={false}
                                                        label={({ name, value }) => `${name}: ${value}`}
                                                        outerRadius={120}
                                                        innerRadius={60}
                                                        fill="#8884d8"
                                                        dataKey="totalActiveMachines"
                                                    >
                                                        {dashboardData.mostActiveApplications.map((_entry, index) => (
                                                            <Cell key={`cell-${index}`} fill={['#1976d2', '#82ca9d', '#ffc658', '#ff7c7c', '#a28ee8', '#ff9f40'][index % 6]} />
                                                        ))}
                                                    </Pie>
                                                    <RechartsTooltip />
                                                    <Legend />
                                                </PieChart>
                                            </ResponsiveContainer>
                                        </Box>

                                        {/* Active machines details table */}
                                        <Box sx={{ mt: 3 }}>
                                            <Typography variant="h6" fontWeight={600} gutterBottom>
                                                Activity Details
                                            </Typography>
                                            <TableContainer>
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 600 }}>Application</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Total Active</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>Today</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>This Week</TableCell>
                                                            <TableCell align="right" sx={{ fontWeight: 600 }}>This Month</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Last Activity</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {dashboardData.mostActiveApplications.map((app, index) => (
                                                            <TableRow key={app.appCode} hover>
                                                                <TableCell>
                                                                    <Box sx={{ display: "flex", alignItems: "center" }}>
                                                                        <Box
                                                                            sx={{
                                                                                width: 12,
                                                                                height: 12,
                                                                                borderRadius: "50%",
                                                                                bgcolor: ['#1976d2', '#82ca9d', '#ffc658', '#ff7c7c', '#a28ee8', '#ff9f40'][index % 6],
                                                                                mr: 1
                                                                            }}
                                                                        />
                                                                        <Box>
                                                                            <Typography variant="body2" fontWeight={600}>
                                                                                {app.applicationName}
                                                                            </Typography>
                                                                            <Typography variant="caption" color="text.secondary">
                                                                                {app.appCode}
                                                                            </Typography>
                                                                        </Box>
                                                                    </Box>
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.totalActiveMachines} size="small" color="primary" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.todayActiveMachines} size="small" variant="outlined" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.weekActiveMachines} size="small" variant="outlined" />
                                                                </TableCell>
                                                                <TableCell align="right">
                                                                    <Chip label={app.monthActiveMachines} size="small" variant="outlined" />
                                                                </TableCell>
                                                                <TableCell>
                                                                    <Typography variant="caption">
                                                                        {app.lastActivityDate ? FormatUtcTime.formatDateTime(app.lastActivityDate) : ''}
                                                                    </Typography>
                                                                </TableCell>
                                                            </TableRow>
                                                        ))}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </>
                        )}
                    </>
                )}

                <Snackbar
                    open={snackbar.open}
                    autoHideDuration={4000}
                    onClose={() => setSnackbar({ ...snackbar, open: false })}
                    anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
                >
                    <Alert
                        onClose={() => setSnackbar({ ...snackbar, open: false })}
                        severity={snackbar.severity}
                        sx={{ width: "100%" }}
                    >
                        {snackbar.message}
                    </Alert>
                </Snackbar>
            </Box>
        </AdminLayout>
    );
}
