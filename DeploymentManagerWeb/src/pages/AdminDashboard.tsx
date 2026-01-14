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
    DoneAll as DoneAllIcon
} from "@mui/icons-material";
import { useState, useEffect } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { dashboardService } from "../services/deploymentManagerService";
import type { AnalyticDashboardResponse } from "../type/dashboardType";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";

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
                                    <Typography variant="h3" fontWeight={700} color="success.main">
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
                                    <Typography variant="h3" fontWeight={700} color="error.main">
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
                                        <TrendingUpIcon color="primary" sx={{ mr: 1 }} />
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

                        <Card>
                            <CardContent>
                                <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
                                    <UpdateIcon color="primary" sx={{ mr: 1 }} />
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
