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
    Collapse,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper,
    Chip,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
} from "@mui/material";
import {
    Refresh as RefreshIcon,
    ExpandMore as ExpandMoreIcon,
    ChevronRight as ChevronRightIcon,
    Download as DownloadIcon,
    Delete as DeleteIcon,
    FileDownload as FileDownloadIcon
} from "@mui/icons-material";
import { useState, useEffect } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { packageManagementService } from "../services/deploymentManagerService";
import type { ApplicationPackageResponse } from "../type/packageManagementType";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";

interface ApplicationWithPackages {
    applicationName: string;
    packages: ApplicationPackageResponse[];
    expanded: boolean;
}

export default function AdminPackagesPage() {
    useSetPageTitle(PAGE_TITLES.PACKAGES);

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingPackage, setDeletingPackage] = useState<string | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    const [loading, setLoading] = useState<boolean>(false);
    const [applications, setApplications] = useState<ApplicationWithPackages[]>([]);
    const [snackbar, setSnackbar] = useState<{
        open: boolean;
        message: string;
        severity: "success" | "error" | "info";
    }>({ open: false, message: "", severity: "info" });

    // Load package history
    const loadPackageHistory = async () => {
        setLoading(true);
        try {
            const response = await packageManagementService.getPackageHistoryByApplication();

            // Transform the response into ApplicationWithPackages array
            // The response is a dictionary with appCode as key: { [appCode: string]: ApplicationPackageResponse[] }
            const appsWithPackages: ApplicationWithPackages[] = Object.entries(response).map(
                ([appCode, packages]) => ({
                    applicationName: packages.length > 0 ? packages[0].applicationName : appCode,
                    packages: packages,
                    expanded: false,
                })
            );

            setApplications(appsWithPackages);
        } catch (error: any) {
            console.error("Error loading package history:", error);
            setSnackbar({
                open: true,
                message: error?.response?.data?.message || "Failed to load package history",
                severity: "error",
            });
        } finally {
            setLoading(false);
        }
    };

    // Handle download package
    const handleDownloadPackage = async (packageId: number, packageName: string) => {
        try {
            setSnackbar({
                open: true,
                message: `Downloading ${packageName}...`,
                severity: "info",
            });

            await packageManagementService.downloadPackage(packageId, packageName);

            setSnackbar({
                open: true,
                message: `${packageName} downloaded successfully`,
                severity: "success",
            });
        } catch (error: any) {
            console.error("Error downloading package:", error);
            setSnackbar({
                open: true,
                message: error?.response?.data?.message || "Failed to download package",
                severity: "error",
            });
        }
    };

    const handleDeletePackage = async (pkg: ApplicationPackageResponse) => {
        debugger;
        try {
            setSnackbar({
                open: true,
                message: `Deleting ${pkg.packageFileName}...`,
                severity: "info",
            });

            await packageManagementService.deletePackage(pkg.id);

            setSnackbar({
                open: true,
                message: `${pkg.packageFileName} deleted successfully`,
                severity: "success",
            });
        } catch (error: any) {
            console.error("Error deleting package:", error);
            setSnackbar({
                open: true,
                message: error?.response?.data?.message || "Failed to delete package",
                severity: "error",
            });
        }
    }

    // Toggle application expansion
    const toggleApplication = (index: number) => {
        setApplications((prev) =>
            prev.map((app, i) =>
                i === index ? { ...app, expanded: !app.expanded } : app
            )
        );
    };

    // Load data on mount
    useEffect(() => {
        loadPackageHistory();
    }, []);

    const handleCloseDeleteDialog = () => {
        setDeleteDialogOpen(false);
        setDeletingPackage(null);
    };

    const handleOpenDeleteDialog = (pkg: ApplicationPackageResponse) => {
        setDeletingPackage(pkg.packageFileName);
        setDeleteDialogOpen(true);
        handleDeletePackage(pkg);
    };

    return (
        <AdminLayout>
            <Box sx={{ p: 3 }}>
                {/* Header */}
                <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3 }}>
                    {/* <Typography variant="h4" fontWeight={700}>
                        Package Management
                    </Typography> */}
                    <Button variant="contained" color="primary" onClick={loadPackageHistory}>
                        Refresh
                    </Button>
                </Box>

                {/* Loading Bar */}
                {loading && <LinearProgress sx={{ mb: 1 }} />}

                {/* Package List */}
                <Card>
                    <CardContent>
                        {applications.length === 0 && !loading ? (
                            <Alert severity="info">No packages found</Alert>
                        ) : (
                            <Box>
                                {applications.map((app, index) => (
                                    <Box key={app.applicationName} sx={{ mb: 1 }}>
                                        {/* Application Header - Clickable */}
                                        <Box
                                            onClick={() => toggleApplication(index)}
                                            sx={{
                                                display: "flex",
                                                alignItems: "center",
                                                cursor: "pointer",
                                                p: 1,
                                                bgcolor: "action.hover",
                                                borderRadius: 1,
                                                transition: "all 0.2s",
                                                "&:hover": {
                                                    bgcolor: "action.selected",
                                                },
                                            }}
                                        >
                                            <IconButton size="small" sx={{ mr: 1 }}>
                                                {app.expanded ? <ExpandMoreIcon /> : <ChevronRightIcon />}
                                            </IconButton>
                                            <Typography variant="body1" fontWeight={600} sx={{ flexGrow: 1 }}>
                                                {app.applicationName}
                                            </Typography>
                                            <Chip
                                                label={`${app.packages.length} package${app.packages.length !== 1 ? "s" : ""}`}
                                                size="small"
                                                color="primary"
                                            />
                                        </Box>

                                        {/* Package List - Collapsible */}
                                        <Collapse in={app.expanded} timeout="auto" unmountOnExit>
                                            <Box sx={{ mt: 2, pl: 6 }}>
                                                <TableContainer component={Paper} variant="outlined">
                                                    <Table size="small">
                                                        <TableHead>
                                                            <TableRow>
                                                                <TableCell>Version</TableCell>
                                                                <TableCell>Type</TableCell>
                                                                <TableCell>File Name</TableCell>
                                                                <TableCell>Size</TableCell>
                                                                <TableCell>Downloads</TableCell>
                                                                <TableCell>Last Downloaded</TableCell>
                                                                <TableCell align="center">Actions</TableCell>
                                                            </TableRow>
                                                        </TableHead>
                                                        <TableBody>
                                                            {app.packages.map((pkg) => (
                                                                <TableRow key={pkg.id} hover>
                                                                    <TableCell>
                                                                        <Chip
                                                                            label={pkg.version}
                                                                            size="small"
                                                                            color="primary"
                                                                            variant="outlined"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Chip
                                                                            label={pkg.packageType}
                                                                            size="small"
                                                                            color={
                                                                                pkg.packageType === "Full"
                                                                                    ? "success"
                                                                                    : pkg.packageType === "Binary"
                                                                                        ? "info"
                                                                                        : "default"
                                                                            }
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>{pkg.packageFileName}</TableCell>
                                                                    <TableCell>{pkg.fileSizeFormatted}</TableCell>
                                                                    <TableCell>{pkg.downloadCount}</TableCell>
                                                                    <TableCell>
                                                                        {pkg.lastDownloadedAt
                                                                            ? FormatUtcTime.formatDateTime(pkg.lastDownloadedAt)
                                                                            : "Never"}
                                                                    </TableCell>
                                                                    <TableCell align="center">
                                                                        <Tooltip title="Download Package">
                                                                            <IconButton
                                                                                size="small"
                                                                                color="success"
                                                                                sx={{ mr: 1 }}
                                                                                onClick={() =>
                                                                                    handleDownloadPackage(
                                                                                        pkg.id,
                                                                                        pkg.packageFileName
                                                                                    )
                                                                                }
                                                                            >
                                                                                <FileDownloadIcon />
                                                                            </IconButton>
                                                                        </Tooltip>

                                                                        <Tooltip title="Delete Package">
                                                                            <IconButton
                                                                                size="small"
                                                                                color="error"
                                                                                onClick={() => handleOpenDeleteDialog(pkg)}
                                                                            >
                                                                                <DeleteIcon />
                                                                            </IconButton>
                                                                        </Tooltip>
                                                                    </TableCell>
                                                                </TableRow>
                                                            ))}
                                                        </TableBody>
                                                    </Table>
                                                </TableContainer>
                                            </Box>
                                        </Collapse>
                                    </Box>
                                ))}
                            </Box>
                        )}
                    </CardContent>
                </Card>

                {/* Delete Confirmation Dialog */}
                <Dialog
                    open={deleteDialogOpen}
                    onClose={handleCloseDeleteDialog}
                    maxWidth="sm"
                    fullWidth
                >
                    <DialogTitle>Confirm Delete</DialogTitle>
                    <DialogContent>
                        <Typography>
                            Are you sure you want to delete the application <strong>"{deletingPackage}"</strong>?
                        </Typography>
                        <Alert severity="warning" sx={{ mt: 2 }}>
                            This action cannot be undone. All associated data will be permanently deleted.
                        </Alert>
                    </DialogContent>
                    <DialogActions>
                        <Button
                            onClick={handleCloseDeleteDialog}
                            sx={{ border: 1 }}
                            disabled={deleteLoading}
                        >
                            Cancel
                        </Button>

                        <Button
                            variant="contained"
                            color="error"
                            disabled={deleteLoading}
                            startIcon={<DeleteIcon />}
                        >
                            {deleteLoading ? "Deleting..." : "Delete"}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Snackbar for notifications */}
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
