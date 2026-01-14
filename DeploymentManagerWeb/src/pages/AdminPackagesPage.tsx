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
    TextField,
    InputAdornment,
    Pagination,
    FormControl,
    InputLabel,
    Select,
    MenuItem,
    Stack,
    Grid,
} from "@mui/material";
import {
    Refresh as RefreshIcon,
    ExpandMore as ExpandMoreIcon,
    ChevronRight as ChevronRightIcon,
    Download as DownloadIcon,
    Delete as DeleteIcon,
    FileDownload as FileDownloadIcon,
    Search as SearchIcon,
    Edit as EditIcon,
    Save as SaveIcon
} from "@mui/icons-material";
import { useState, useEffect } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { packageManagementService } from "../services/deploymentManagerService";
import type { ApplicationPackageResponse, PackageVersionResponse } from "../type/packageManagementType";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { extractErrorMessage } from "../utils/errorHandler";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";

interface ApplicationWithPackages {
    applicationName: string;
    appCode: string;
    packages: ApplicationPackageResponse[];
    expanded: boolean;
}

export default function AdminPackagesPage() {
    useSetPageTitle(PAGE_TITLES.PACKAGES);

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingPackage, setDeletingPackage] = useState<ApplicationPackageResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    const [loading, setLoading] = useState<boolean>(false);
    const [applications, setApplications] = useState<ApplicationWithPackages[]>([]);
    const [snackbar, setSnackbar] = useState<{
        open: boolean;
        message: string;
        severity: "success" | "error" | "info";
    }>({ open: false, message: "", severity: "info" });

    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);

    // Dialog
    const [dialogOpen, setDialogOpen] = useState(false);
    const [dialogLoading, setDialogLoading] = useState(false);
    const [editingPackage, setEditingPackage] = useState<ApplicationPackageResponse | null>(null);

    // Filtered and paginated data
    const filteredApplications = applications.filter((app) => {
        const searchLower = searchTerm.toLowerCase();
        const appNameMatch = app.applicationName.toLowerCase().includes(searchLower);
        const appCodeMatch = app.appCode.toLowerCase().includes(searchLower);
        return appNameMatch || appCodeMatch;
    });

    const totalPages = Math.ceil(filteredApplications.length / itemsPerPage);
    const paginatedApplications = filteredApplications.slice(
        (currentPage - 1) * itemsPerPage,
        currentPage * itemsPerPage
    );

    // Form states - Package Upload
    const [packageFormData, setPackageFormData] = useState({
        Id: 0,
        Version: "",
        ReleaseNotes: "",
    });
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [formErrors, setFormErrors] = useState({
        file: false,
        releaseNotes: false,
    });

    // Reset to page 1 when search term changes
    const handleSearchChange = (value: string) => {
        setSearchTerm(value);
        setCurrentPage(1);
    };

    // Reset to page 1 when items per page changes
    const handleItemsPerPageChange = (value: number) => {
        setItemsPerPage(value);
        setCurrentPage(1);
    };

    // Load package history
    const loadPackageHistory = async () => {
        setLoading(true);
        try {
            const response = await packageManagementService.getPackageHistoryByApplication();

            // Transform the response into ApplicationWithPackages array
            // The response is a dictionary with appCode as key: { [appCode: string]: ApplicationPackageResponse[] }
            const appsWithPackages: ApplicationWithPackages[] = Object.entries(response).map(
                ([appCode, packages]) => ({
                    appCode: appCode,
                    applicationName: packages.length > 0 ? packages[0].applicationName : appCode,
                    packages: packages,
                    expanded: false,
                })
            );

            setApplications(appsWithPackages);
        } catch (error: any) {
            console.error("Error loading package history:", error);
            const errorMessage = extractErrorMessage(error, "Failed to load package history");
            setSnackbar({
                open: true,
                message: errorMessage,
                severity: "error",
            });
        } finally {
            setLoading(false);
        }
    };

    const handleSubmit = async () => {
        if (!editingPackage) return;

        // Validate
        const errors = {
            file: !selectedFile,
            releaseNotes: !packageFormData.ReleaseNotes.trim(),
        };
        setFormErrors(errors);

        if (errors.file) {
            setSnackbar({
                open: true,
                message: "Package file is required",
                severity: "error",
            });
            return;
        }

        if (errors.releaseNotes) {
            setSnackbar({
                open: true,
                message: "Release notes are required",
                severity: "error",
            });
            return;
        }

        setDialogLoading(true);
        try {
            const formData = new FormData();
            formData.append('Id', editingPackage.id.toString());
            formData.append('ApplicationId', editingPackage.applicationId.toString());
            formData.append('ReleaseNotes', packageFormData.ReleaseNotes);

            if (selectedFile) {
                formData.append('NewPackage', selectedFile);
            }

            await packageManagementService.updatePackage(formData);

            setSnackbar({
                open: true,
                message: `Package updated successfully!`,
                severity: "success",
            });

            handleCloseDialog();
            loadPackageHistory();
        } catch (error: any) {
            console.error("Error updating package:", error);
            const errorMessage = extractErrorMessage(error, "Failed to update package");
            setSnackbar({
                open: true,
                message: errorMessage,
                severity: "error",
            });
        } finally {
            setDialogLoading(false);
        }
    }

    const handleOpenEditDialog = async (pkg: ApplicationPackageResponse) => {
        setEditingPackage(pkg);
        setPackageFormData({
            Id: pkg.id,
            Version: pkg.version,
            ReleaseNotes: "",
        });
        setSelectedFile(null);
        setFormErrors({ file: false, releaseNotes: false });
        setDialogOpen(true);
    }

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
            const errorMessage = extractErrorMessage(error, "Failed to download package");
            setSnackbar({
                open: true,
                message: errorMessage,
                severity: "error",
            });
        }
    };

    const handleDeletePackage = async () => {
        if (!deletingPackage) return;
        try {
            setSnackbar({
                open: true,
                message: `Deleting ${deletingPackage?.packageFileName}...`,
                severity: "info",
            });

            await packageManagementService.deletePackage(deletingPackage.id);
            setSnackbar({
                open: true,
                message: `${deletingPackage?.packageFileName} deleted successfully`,
                severity: "success",
            });
            handleCloseDeleteDialog();
        } catch (error: any) {
            console.error("Error deleting package:", error);
            const errorMessage = extractErrorMessage(error, "Error deleting package");
            setSnackbar({ open: true, message: errorMessage, severity: "error" });
            throw error;
        } finally {
            setDeleteLoading(false);
            loadPackageHistory();
        }
    }

    // Toggle application expansion
    const toggleApplication = (appCode: string) => {
        setApplications((prev) =>
            prev.map((app) =>
                app.appCode === appCode ? { ...app, expanded: !app.expanded } : app
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
        setDeleteDialogOpen(true);
        setDeletingPackage(pkg);
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingPackage(null);
        setSelectedFile(null);
        setPackageFormData({
            Id: 0,
            Version: "",
            ReleaseNotes: "",
        });
        setFormErrors({ file: false, releaseNotes: false });
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files[0]) {
            setSelectedFile(event.target.files[0]);
            setFormErrors(prev => ({ ...prev, file: false }));
        }
    };

    const handleReleaseNotesChange = (value: string) => {
        setPackageFormData(prev => ({ ...prev, ReleaseNotes: value }));
        if (formErrors.releaseNotes) {
            setFormErrors(prev => ({ ...prev, releaseNotes: false }));
        }
    };

    return (
        <AdminLayout>
            <Box sx={{ p: 3 }}>
                {/* Header */}
                <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 3, gap: 2 }}>
                    <Stack direction="row" spacing={2} sx={{ flexGrow: 1 }}>
                        {/* Search Bar */}
                        <TextField
                            placeholder="Search by application name or code..."
                            size="small"
                            value={searchTerm}
                            onChange={(e) => handleSearchChange(e.target.value)}
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <SearchIcon />
                                    </InputAdornment>
                                ),
                            }}
                            sx={{ flexGrow: 1, maxWidth: 400 }}
                        />

                        <Button variant="contained" color="primary" onClick={loadPackageHistory} startIcon={<RefreshIcon />}>
                            Refresh
                        </Button>
                    </Stack>

                    {/* Items per page selector */}
                    <FormControl size="small" sx={{ minWidth: 120 }}>
                        <InputLabel>Rows/Page</InputLabel>
                        <Select
                            value={itemsPerPage}
                            label="Rows/Page"
                            onChange={(e) => handleItemsPerPageChange(Number(e.target.value))}
                        >
                            <MenuItem value={5}>5</MenuItem>
                            <MenuItem value={10}>10</MenuItem>
                            <MenuItem value={20}>20</MenuItem>
                            <MenuItem value={50}>50</MenuItem>
                        </Select>
                    </FormControl>
                </Box>

                {/* Loading Bar */}
                {loading && <LinearProgress sx={{ mb: 1 }} />}

                {/* Package List */}
                <Card>
                    <CardContent>
                        {/* Result count */}
                        <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <Typography variant="body2" color="text.secondary">
                                Showing {paginatedApplications.length > 0 ? (currentPage - 1) * itemsPerPage + 1 : 0} - {Math.min(currentPage * itemsPerPage, filteredApplications.length)} of {filteredApplications.length} applications
                                {searchTerm && ` (filtered from ${applications.length} total)`}
                            </Typography>
                        </Box>

                        {filteredApplications.length === 0 && !loading ? (
                            <Alert severity="info">
                                {searchTerm ? `No applications found matching "${searchTerm}"` : "No packages found"}
                            </Alert>
                        ) : (
                            <Box>
                                {paginatedApplications.map((app) => (
                                    <Box key={app.appCode} sx={{ mb: 1 }}>
                                        {/* Application Header - Clickable */}
                                        <Box
                                            onClick={() => toggleApplication(app.appCode)}
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
                                            <Box sx={{ flexGrow: 1 }}>
                                                <Typography variant="body1" fontWeight={600}>
                                                    {app.applicationName}
                                                </Typography>
                                                <Typography variant="caption" color="text.secondary">
                                                    Code: {app.appCode}
                                                </Typography>
                                            </Box>
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
                                                                <TableCell>Package Changed</TableCell>
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
                                                                    <TableCell>
                                                                        {pkg.replacesVersionNumber && pkg.replacesVersionId ?
                                                                            <Chip label={`YES - From version: ${pkg.replacesVersionNumber}`} size="small" color="warning" /> : <Chip label="NO" size="small" color="default" />}
                                                                    </TableCell>
                                                                    <TableCell align="center">
                                                                        <Tooltip title="Update Package">
                                                                            <IconButton
                                                                                size="small"
                                                                                color="primary"
                                                                                sx={{ mr: 1 }}
                                                                                onClick={() =>
                                                                                    handleOpenEditDialog(pkg)
                                                                                }
                                                                            >
                                                                                <EditIcon />
                                                                            </IconButton>
                                                                        </Tooltip>
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
                                                                                disabled={pkg.applicationName === 'Client Application'}
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



                        {/* Pagination */}
                        {filteredApplications.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
                                <Pagination
                                    count={totalPages}
                                    page={currentPage}
                                    onChange={(_, page) => setCurrentPage(page)}
                                    color="primary"
                                    showFirstButton
                                    showLastButton
                                />
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
                            Are you sure you want to delete the package <strong>"{deletingPackage?.packageFileName}"</strong> of the application <strong>"{deletingPackage?.applicationName}"</strong>?
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
                            onClick={handleDeletePackage}
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

            {/* Update Package Dialog */}
            <Dialog
                open={dialogOpen}
                onClose={handleCloseDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    Update Package
                </DialogTitle>
                <DialogContent>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 2 }}>
                        <Alert severity="info">
                            Upload a new package file for this version. The version number cannot be changed.
                        </Alert>

                        {/* Display Version (Read-only) */}
                        <TextField
                            label="Version"
                            fullWidth
                            value={packageFormData.Version}
                            disabled
                            helperText="Version cannot be changed"
                        />

                        {/* Release Notes */}
                        <TextField
                            label="Release Notes"
                            fullWidth
                            required
                            multiline
                            rows={4}
                            value={packageFormData.ReleaseNotes}
                            onChange={(e) => handleReleaseNotesChange(e.target.value)}
                            error={formErrors.releaseNotes}
                            helperText={formErrors.releaseNotes ? "Release notes are required" : "Describe what's new in this package"}
                            disabled={dialogLoading}
                        />

                        {/* File Upload */}
                        <Box>
                            <Button
                                variant="outlined"
                                component="label"
                                fullWidth
                                startIcon={<FileDownloadIcon />}
                                disabled={dialogLoading}
                                color={formErrors.file ? "error" : "primary"}
                            >
                                {selectedFile ? selectedFile.name : "Select New Package File *"}
                                <input
                                    type="file"
                                    hidden
                                    accept=".zip,.rar,.7z,.tar,.gz"
                                    onChange={handleFileChange}
                                />
                            </Button>
                            {formErrors.file && (
                                <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
                                    Package file is required
                                </Typography>
                            )}
                            {selectedFile && (
                                <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                                    Selected file: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
                                </Typography>
                            )}
                        </Box>

                        {editingPackage && (
                            <Alert severity="warning" sx={{ mt: 1 }}>
                                <Typography variant="body2" fontWeight={600}>Current Package Info:</Typography>
                                <Typography variant="caption" display="block">File: {editingPackage.packageFileName}</Typography>
                                <Typography variant="caption" display="block">Size: {editingPackage.fileSizeFormatted}</Typography>
                                <Typography variant="caption" display="block">Downloads: {editingPackage.downloadCount}</Typography>
                            </Alert>
                        )}
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button
                        onClick={handleCloseDialog}
                        disabled={dialogLoading}
                    >
                        Cancel
                    </Button>
                    <Button
                        onClick={handleSubmit}
                        variant="contained"
                        disabled={dialogLoading}
                        startIcon={<SaveIcon />}
                    >
                        {dialogLoading ? "Updating..." : "Update Package"}
                    </Button>
                </DialogActions>
            </Dialog>


        </AdminLayout>
    );
}
