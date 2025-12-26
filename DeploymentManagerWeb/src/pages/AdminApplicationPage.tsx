import {
    Box,
    Card,
    CardContent,
    Typography,
    Grid,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper,
    TextField,
    Button,
    FormControl,
    InputLabel,
    Select,
    MenuItem,
    LinearProgress,
    Alert,
    Skeleton,
    Pagination,
    Switch,
    Chip,
    IconButton,
    Tooltip,
    Snackbar,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Tabs,
    Tab,
    FormControlLabel,
    Checkbox,
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    Add as AddIcon,
    Edit as EditIcon,
    Delete as DeleteIcon,
    Save as SaveIcon,
    Visibility as VisibilityIcon,
    Upload as UploadIcon,
    AttachFile as AttachFileIcon,
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { applicationService, categoryService, packageManagementService } from "../services/deploymentManagerService";
import type { ApplicationResponse, CreateApplicationRequest, UpdateApplicationRequest } from "../type/applicationType";
import type { CategoryResponse } from "../type/categoryType";
import type { ManifestCreateRequest, ManifestResponse } from "../type/manifestType";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

interface TabPanelProps {
    children?: React.ReactNode;
    index: number;
    value: number;
}

function TabPanel(props: TabPanelProps) {
    const { children, value, index, ...other } = props;
    return (
        <div role="tabpanel" hidden={value !== index} {...other}>
            {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
        </div>
    );
}

export default function AdminApplicationPage() {
    useSetPageTitle(PAGE_TITLES.APPLICATIONS);
    const [applications, setApplications] = useState<ApplicationResponse[]>([]);
    const [categories, setCategories] = useState<CategoryResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [toggleLoading, setToggleLoading] = useState<number | null>(null);

    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" as "success" | "error" });

    // Dialog states
    const [dialogOpen, setDialogOpen] = useState(false);
    const [dialogMode, setDialogMode] = useState<"create" | "edit">("create");
    const [dialogLoading, setDialogLoading] = useState(false);
    const [editingApplication, setEditingApplication] = useState<ApplicationResponse | null>(null);
    const [tabValue, setTabValue] = useState(0);

    // Form states - Application
    const [appFormData, setAppFormData] = useState({
        appCode: "",
        name: "",
        description: "",
        iconUrl: "",
        categoryId: 0,
    });

    // Form states - Manifest
    const [manifestFormData, setManifestFormData] = useState<ManifestCreateRequest>({
        Version: "",
        BinaryVersion: "",
        BinaryPackage: "",
        ConfigVersion: "",
        ConfigPackage: "",
        ConfigMergeStrategy: "Replace",
        UpdateType: "Optional",
        ForceUpdate: false,
        ReleaseNotes: "",
        IsStable: true,
        PublishedAt: new Date().toISOString().slice(0, 16),
    });

    // Form states - Package Upload
    const [packageFormData, setPackageFormData] = useState({
        Version: "",
        PackageType: "Binary",
        ReleaseNotes: "",
        IsStable: true,
        MinimumClientVersion: "",
        PublishImmediately: true,
    });
    const [selectedFile, setSelectedFile] = useState<File | null>(null);

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingApplication, setDeletingApplication] = useState<ApplicationResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    // View manifest dialog
    const [viewManifestDialogOpen, setViewManifestDialogOpen] = useState(false);
    const [viewingManifest, setViewingManifest] = useState<ManifestResponse | null>(null);
    const [manifestLoading, setManifestLoading] = useState(false);
    const [editingManifest, setEditingManifest] = useState(false);

    const showSnackbar = (message: string, severity: "success" | "error") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadApplications = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await applicationService.getAllApplications();
            setApplications(data);
        } catch (error: any) {
            console.error("Error loading applications:", error);
            let errorMessage = "Failed to load applications data";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            setError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const loadCategories = async () => {
        try {
            const data = await categoryService.getAllCategories();
            setCategories(data.filter(c => c.isActive));
        } catch (error: any) {
            console.error("Error loading categories:", error);
            showSnackbar("Failed to load categories", "error");
        }
    };

    const handleOpenCreateDialog = () => {
        setDialogMode("create");
        setEditingApplication(null);
        setTabValue(0);
        setAppFormData({
            appCode: "",
            name: "",
            description: "",
            iconUrl: "",
            categoryId: 0,
        });
        setManifestFormData({
            Version: "",
            BinaryVersion: "",
            BinaryPackage: "",
            ConfigVersion: "",
            ConfigPackage: "",
            ConfigMergeStrategy: "Replace",
            UpdateType: "Optional",
            ForceUpdate: false,
            ReleaseNotes: "",
            IsStable: true,
            PublishedAt: new Date().toISOString().slice(0, 16),
        });
        setPackageFormData({
            Version: "",
            PackageType: "Binary",
            ReleaseNotes: "",
            IsStable: true,
            MinimumClientVersion: "",
            PublishImmediately: true,
        });
        setSelectedFile(null);
        setDialogOpen(true);
        loadCategories();
    };

    const handleOpenEditDialog = async (application: ApplicationResponse) => {
        setDialogMode("edit");
        setEditingApplication(application);
        setTabValue(0);
        setAppFormData({
            appCode: application.appCode,
            name: application.name,
            description: application.description,
            iconUrl: application.iconUrl,
            categoryId: application.categoryId,
        });
        setPackageFormData({
            Version: "",
            PackageType: "Binary",
            ReleaseNotes: "",
            IsStable: true,
            MinimumClientVersion: "",
            PublishImmediately: true,
        });
        setSelectedFile(null);

        // Load manifest if exists
        try {
            const manifest = await applicationService.getApplicationManifest(application.id);
            if (manifest) {
                setManifestFormData({
                    Version: manifest.version || "",
                    BinaryVersion: manifest.binaryVersion || "",
                    BinaryPackage: manifest.binaryPackage || "",
                    ConfigVersion: manifest.configVersion || "",
                    ConfigPackage: manifest.configPackage || "",
                    ConfigMergeStrategy: manifest.configMergeStrategy || "Replace",
                    UpdateType: manifest.updateType || "Optional",
                    ForceUpdate: manifest.forceUpdate || false,
                    ReleaseNotes: manifest.releaseNotes || "",
                    IsStable: manifest.isStable !== undefined ? manifest.isStable : true,
                    PublishedAt: manifest.publishedAt ? new Date(manifest.publishedAt).toISOString().slice(0, 16) : new Date().toISOString().slice(0, 16),
                });
            }
        } catch (error) {
            // No manifest exists, keep default values
            setManifestFormData({
                Version: "",
                BinaryVersion: "",
                BinaryPackage: "",
                ConfigVersion: "",
                ConfigPackage: "",
                ConfigMergeStrategy: "Replace",
                UpdateType: "Optional",
                ForceUpdate: false,
                ReleaseNotes: "",
                IsStable: true,
                PublishedAt: new Date().toISOString().slice(0, 16),
            });
        }

        setDialogOpen(true);
        loadCategories();
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingApplication(null);
        setTabValue(0);
    };

    const handleAppFormChange = (field: string, value: string | number) => {
        setAppFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleManifestFormChange = (field: string, value: string | number | boolean) => {
        setManifestFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handlePackageFormChange = (field: string, value: string | boolean) => {
        setPackageFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files[0]) {
            setSelectedFile(event.target.files[0]);
        }
    };

    const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
        setTabValue(newValue);
    };

    const validateAppForm = (): boolean => {
        if (!appFormData.appCode.trim()) {
            showSnackbar("App Code is required", "error");
            return false;
        }
        if (!appFormData.name.trim()) {
            showSnackbar("Application name is required", "error");
            return false;
        }
        if (!appFormData.categoryId || appFormData.categoryId === 0) {
            showSnackbar("Category is required", "error");
            return false;
        }
        return true;
    };

    const validateManifestForm = (): boolean => {
        if (!manifestFormData.Version.trim()) {
            showSnackbar("Version is required", "error");
            return false;
        }
        if (!manifestFormData.BinaryVersion.trim()) {
            showSnackbar("Binary Version is required", "error");
            return false;
        }
        if (!manifestFormData.BinaryPackage.trim()) {
            showSnackbar("Binary Package is required", "error");
            return false;
        }
        return true;
    };

    const handleSubmit = async () => {
        if (dialogMode === "create") {
            // For create mode, validate app form
            if (!validateAppForm()) {
                setTabValue(0); // Switch to app tab
                return;
            }

            try {
                setDialogLoading(true);

                // Step 1: Create application
                const appRequest: CreateApplicationRequest = {
                    appCode: appFormData.appCode.trim(),
                    name: appFormData.name.trim(),
                    description: appFormData.description.trim(),
                    iconUrl: appFormData.iconUrl.trim(),
                    categoryId: appFormData.categoryId,
                };
                const createdApp = await applicationService.createApplication(appRequest);
                showSnackbar(`Application "${createdApp.name}" created successfully!`, "success");

                // Step 2: Create manifest if form is filled
                if (manifestFormData.Version.trim()) {
                    if (!validateManifestForm()) {
                        setTabValue(1); // Switch to manifest tab
                        setApplications(prev => [...prev, createdApp]);
                        setDialogLoading(false);
                        return;
                    }

                    try {
                        await applicationService.createApplicationManifest(createdApp.id, manifestFormData);
                        showSnackbar(`Manifest for "${createdApp.name}" created successfully!`, "success");
                    } catch (error: any) {
                        console.error("Error creating manifest:", error);
                        showSnackbar(`Application created but failed to create manifest: ${error?.response?.data?.message || error?.message}`, "error");
                    }
                }

                // Step 3: Upload package if file is selected
                if (selectedFile) {
                    try {
                        const formData = new FormData();
                        formData.append('ApplicationId', createdApp.id.toString());
                        formData.append('Version', packageFormData.Version);
                        formData.append('PackageType', packageFormData.PackageType);
                        formData.append('PackageFile', selectedFile);
                        formData.append('ReleaseNotes', packageFormData.ReleaseNotes);
                        formData.append('IsStable', packageFormData.IsStable.toString());
                        formData.append('MinimumClientVersion', packageFormData.MinimumClientVersion);
                        formData.append('PublishImmediately', packageFormData.PublishImmediately.toString());

                        // Get current user from localStorage
                        const user = JSON.parse(localStorage.getItem('user') || '{}');
                        formData.append('UploadedBy', user.username || 'Unknown');

                        await packageManagementService.uploadPackage(formData);
                        showSnackbar(`Package uploaded successfully!`, "success");
                    } catch (error: any) {
                        console.error("Error uploading package:", error);
                        showSnackbar(`Application created but failed to upload package: ${error?.response?.data?.message || error?.message}`, "error");
                    }
                }

                setApplications(prev => [...prev, createdApp]);
                handleCloseDialog();
                loadApplications(); // Reload to get updated data
            } catch (error: any) {
                console.error("Error creating application:", error);
                let errorMessage = "Error creating application";
                if (error?.response?.data?.message) {
                    errorMessage = error.response.data.message;
                } else if (error?.message) {
                    errorMessage = error.message;
                }
                showSnackbar(errorMessage, "error");
            } finally {
                setDialogLoading(false);
            }
        } else {
            // Edit mode - update application, manifest, and optionally upload package
            if (!validateAppForm()) {
                return;
            }

            if (!editingApplication) return;

            try {
                setDialogLoading(true);

                // Step 1: Update application
                const updateRequest: UpdateApplicationRequest = {
                    name: appFormData.name.trim(),
                    description: appFormData.description.trim(),
                    iconUrl: appFormData.iconUrl.trim(),
                    categoryId: appFormData.categoryId,
                };
                const result = await applicationService.updateApplication(editingApplication.id, updateRequest);
                showSnackbar(`Application "${result.name}" updated successfully!`, "success");

                // Step 2: Update manifest if form is filled
                if (manifestFormData.Version.trim()) {
                    if (!validateManifestForm()) {
                        setTabValue(1); // Switch to manifest tab
                        setApplications(prev =>
                            prev.map(app => app.id === result.id ? result : app)
                        );
                        setDialogLoading(false);
                        return;
                    }

                    try {
                        await applicationService.updateApplicationManifest(editingApplication.id, editingApplication.manifestId, manifestFormData);
                        showSnackbar(`Manifest updated successfully!`, "success");
                    } catch (error: any) {
                        console.error("Error updating manifest:", error);
                        // Try to create if update fails (manifest might not exist)
                        try {
                            await applicationService.createApplicationManifest(editingApplication.id, manifestFormData);
                            showSnackbar(`Manifest created successfully!`, "success");
                        } catch (createError: any) {
                            showSnackbar(`Failed to save manifest: ${createError?.response?.data?.message || createError?.message}`, "error");
                        }
                    }
                }

                // Step 3: Upload package if file is selected
                if (selectedFile) {
                    try {
                        debugger;
                        const formData = new FormData();
                        formData.append('ApplicationId', editingApplication.id.toString());
                        formData.append('Version', packageFormData.Version);
                        formData.append('PackageType', packageFormData.PackageType);
                        formData.append('PackageFile', selectedFile);
                        formData.append('ReleaseNotes', packageFormData.ReleaseNotes);
                        formData.append('IsStable', packageFormData.IsStable.toString());
                        formData.append('MinimumClientVersion', packageFormData.MinimumClientVersion);
                        formData.append('PublishImmediately', packageFormData.PublishImmediately.toString());

                        formData.append('UploadedBy', 'admin'); // Replace with actual user

                        await packageManagementService.uploadPackage(formData);
                        showSnackbar(`Package uploaded successfully!`, "success");
                    } catch (error: any) {
                        console.error("Error uploading package:", error);
                        showSnackbar(`Application updated but failed to upload package: ${error?.response?.data?.message || error?.message}`, "error");
                    }
                }

                setApplications(prev =>
                    prev.map(app => app.id === result.id ? result : app)
                );
                handleCloseDialog();
                loadApplications(); // Reload to get updated data
            } catch (error: any) {
                console.error("Error updating application:", error);
                let errorMessage = "Error updating application";
                if (error?.response?.data?.message) {
                    errorMessage = error.response.data.message;
                } else if (error?.message) {
                    errorMessage = error.message;
                }
                showSnackbar(errorMessage, "error");
            } finally {
                setDialogLoading(false);
            }
        }
    };

    const handleOpenDeleteDialog = (application: ApplicationResponse) => {
        setDeletingApplication(application);
        setDeleteDialogOpen(true);
    };

    const handleCloseDeleteDialog = () => {
        setDeleteDialogOpen(false);
        setDeletingApplication(null);
    };

    const handleDelete = async () => {
        if (!deletingApplication) return;

        try {
            setDeleteLoading(true);
            await applicationService.deleteApplication(deletingApplication.id);
            setApplications(prev => prev.filter(app => app.id !== deletingApplication.id));
            showSnackbar(`Application "${deletingApplication.name}" deleted successfully!`, "success");
            handleCloseDeleteDialog();
        } catch (error: any) {
            console.error("Error deleting application:", error);
            let errorMessage = "Error deleting application";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setDeleteLoading(false);
        }
    };

    const handleViewManifest = async (application: ApplicationResponse) => {
        try {
            setManifestLoading(true);
            setEditingManifest(false);
            setViewManifestDialogOpen(true);
            const manifest = await applicationService.getApplicationManifest(application.id);
            setViewingManifest(manifest);
        } catch (error: any) {
            console.error("Error loading manifest:", error);
            showSnackbar("Failed to load manifest", "error");
            setViewManifestDialogOpen(false);
        } finally {
            setManifestLoading(false);
        }
    };

    const handleCloseViewManifestDialog = () => {
        setViewManifestDialogOpen(false);
        setViewingManifest(null);
        setEditingManifest(false);
    };

    const handleEditManifest = () => {
        setEditingManifest(true);
    };

    const handleSaveManifest = async () => {
        if (!viewingManifest) return;

        try {
            setManifestLoading(true);
            const updatedManifest = await applicationService.updateApplicationManifest(
                viewingManifest.applicationId,
                viewingManifest.id,
                {
                    Version: viewingManifest.version,
                    BinaryVersion: viewingManifest.binaryVersion,
                    BinaryPackage: viewingManifest.binaryPackage,
                    ConfigVersion: viewingManifest.configVersion,
                    ConfigPackage: viewingManifest.configPackage,
                    ConfigMergeStrategy: viewingManifest.configMergeStrategy,
                    UpdateType: viewingManifest.updateType,
                    ForceUpdate: viewingManifest.forceUpdate,
                    ReleaseNotes: viewingManifest.releaseNotes,
                    IsStable: viewingManifest.isStable,
                    PublishedAt: viewingManifest.publishedAt,
                }
            );
            setViewingManifest(updatedManifest);
            setEditingManifest(false);
            showSnackbar("Manifest updated successfully!", "success");
            loadApplications(); // Reload to get updated data
        } catch (error: any) {
            console.error("Error updating manifest:", error);
            showSnackbar(`Failed to update manifest: ${error?.response?.data?.message || error?.message}`, "error");
        } finally {
            setManifestLoading(false);
        }
    };

    const handleManifestFieldChange = (field: keyof ManifestResponse, value: any) => {
        if (!viewingManifest) return;
        setViewingManifest({
            ...viewingManifest,
            [field]: value
        });
    };

    // Auto load data on component mount
    useEffect(() => {
        loadApplications();
    }, []);

    // Search and pagination logic
    const filteredApplications = useMemo(() => {
        if (!searchTerm) return applications;

        return applications.filter(app =>
            app.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            app.appCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
            app.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
            app.categoryName.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }, [applications, searchTerm]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredApplications.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredApplications.slice(startIndex, endIndex);

    // Reset to first page when search term or items per page changes
    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, itemsPerPage]);

    const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
        setCurrentPage(value);
    };

    const handleItemsPerPageChange = (event: any) => {
        setItemsPerPage(event.target.value);
        setCurrentPage(1);
    };

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(event.target.value);
    };

    return (
        <AdminLayout>
            <Box>
                {/* Header */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" color="text.secondary">
                        Manage applications, create new apps, update information, and manage manifests
                    </Typography>
                </Box>

                {/* Search and Actions */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Grid container spacing={3} alignItems="center">
                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="Search applications..."
                                    value={searchTerm}
                                    onChange={handleSearchChange}
                                    InputProps={{
                                        startAdornment: <SearchIcon sx={{ color: 'text.secondary', mr: 1 }} />
                                    }}
                                />
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <FormControl fullWidth size="small">
                                    <InputLabel>Items per page</InputLabel>
                                    <Select
                                        value={itemsPerPage}
                                        label="Items per page"
                                        onChange={handleItemsPerPageChange}
                                    >
                                        <MenuItem value={5}>5</MenuItem>
                                        <MenuItem value={10}>10</MenuItem>
                                        <MenuItem value={20}>20</MenuItem>
                                        <MenuItem value={50}>50</MenuItem>
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <Button
                                    variant="contained"
                                    fullWidth
                                    startIcon={<RefreshIcon />}
                                    onClick={loadApplications}
                                    disabled={loading}
                                >
                                    Refresh
                                </Button>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <Button
                                    variant="contained"
                                    color="success"
                                    fullWidth
                                    startIcon={<AddIcon />}
                                    onClick={handleOpenCreateDialog}
                                >
                                    Create New
                                </Button>
                            </Grid>

                            <Grid size={{ xs: 12, md: 3 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Total: {filteredApplications.length} applications
                                    {searchTerm && ` (filtered from ${applications.length})`}
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Active: ${applications.filter(app => app.isActive).length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Inactive: ${applications.filter(app => !app.isActive).length}`}
                                        color="error"
                                        size="small"
                                        variant="outlined"
                                    />
                                </Box>
                            </Grid>
                        </Grid>
                    </CardContent>
                </Card>

                {loading && <LinearProgress sx={{ mb: 2 }} />}

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }} action={
                        <IconButton size="small" onClick={loadApplications}>
                            <RefreshIcon />
                        </IconButton>
                    }>
                        {error}
                    </Alert>
                )}

                {/* Applications Table */}
                <Card>
                    <CardContent sx={{ p: 0 }}>
                        <TableContainer component={Paper} variant="outlined">
                            <Table>
                                <TableHead>
                                    <TableRow sx={{ bgcolor: 'grey.50' }}>
                                        <TableCell sx={{
                                            fontWeight: 600,
                                            textAlign: 'center',
                                            position: 'sticky',
                                            left: 0,
                                            backgroundColor: 'background.paper',
                                            zIndex: 3,
                                            boxShadow: '2px 0 5px rgba(0,0,0,0.1)',
                                            borderRight: '1px solid #e0e0e0'
                                        }}>
                                            Actions
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 80 }}>
                                            ID
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            App Code
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 250 }}>
                                            Description
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 150 }}>
                                            Category
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Latest Version
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Total Versions
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Total Installs
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Status
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Updated At
                                        </TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {loading ? (
                                        Array.from({ length: itemsPerPage }).map((_, index) => (
                                            <TableRow key={index}>
                                                <TableCell align="center"><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="40px" /></TableCell>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="200px" /></TableCell>
                                                <TableCell><Skeleton width="120px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                            </TableRow>
                                        ))
                                    ) : currentPageData.length > 0 ? (
                                        currentPageData.map((application) => (
                                            <TableRow
                                                key={application.id}
                                                sx={{
                                                    '&:nth-of-type(even)': { bgcolor: '#f8f9fa' },
                                                    '&:hover': { bgcolor: '#e3f2fd' }
                                                }}
                                            >
                                                <TableCell sx={{
                                                    fontWeight: 600,
                                                    textAlign: 'center',
                                                    position: 'sticky',
                                                    left: 0,
                                                    backgroundColor: 'background.paper',
                                                    zIndex: 3,
                                                    boxShadow: '2px 0 5px rgba(0,0,0,0.1)',
                                                    borderRight: '1px solid #e0e0e0'
                                                }}>
                                                    <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                                                        <Tooltip title="View manifest">
                                                            <IconButton
                                                                size="small"
                                                                color="info"
                                                                onClick={() => handleViewManifest(application)}
                                                            >
                                                                <VisibilityIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                        <Tooltip title="Edit application">
                                                            <IconButton
                                                                size="small"
                                                                color="primary"
                                                                onClick={() => handleOpenEditDialog(application)}
                                                            >
                                                                <EditIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                        <Tooltip title="Delete application">
                                                            <IconButton
                                                                size="small"
                                                                color="error"
                                                                onClick={() => handleOpenDeleteDialog(application)}
                                                            >
                                                                <DeleteIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </Box>
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {application.id}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {application.appCode}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {application.name}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {application.description}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.categoryName}
                                                        color="primary"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {application.latestVersion || 'N/A'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.totalVersions}
                                                        color="info"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.totalInstalls}
                                                        color="success"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.isActive ? "Active" : "Inactive"}
                                                        color={application.isActive ? "success" : "error"}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(application.updatedAt)}
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={11} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    {searchTerm ? `No applications found matching "${searchTerm}"` : "No applications available"}
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {filteredApplications.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {filteredApplications.length > 0 ? startIndex + 1 : 0}-{Math.min(endIndex, filteredApplications.length)} of {filteredApplications.length} items
                                </Typography>
                                {filteredApplications.length > itemsPerPage && (
                                    <Pagination
                                        count={totalPages}
                                        page={currentPage}
                                        onChange={handlePageChange}
                                        color="primary"
                                        size="small"
                                    />
                                )}
                            </Box>
                        )}
                    </CardContent>
                </Card>
            </Box>

            {/* Create/Edit Dialog with Tabs */}
            <Dialog
                open={dialogOpen}
                onClose={handleCloseDialog}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle>
                    {dialogMode === "create" ? "Create New Application" : "Edit Application"}
                </DialogTitle>
                <DialogContent>
                    <Tabs value={tabValue} onChange={handleTabChange} sx={{ borderBottom: 1, borderColor: 'divider' }}>
                        <Tab label="Application Info" />
                        <Tab label={`Manifest ${dialogMode === "create" ? "(Optional)" : ""}`} />
                        <Tab label="Upload Package (Optional)" />
                    </Tabs>

                    <TabPanel value={tabValue} index={0}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            <TextField
                                label="App Code"
                                fullWidth
                                required
                                value={appFormData.appCode}
                                onChange={(e) => handleAppFormChange("appCode", e.target.value)}
                                disabled={dialogLoading || dialogMode === "edit"}
                                autoFocus
                                helperText="Unique identifier for the application"
                            />
                            <TextField
                                label="Application Name"
                                fullWidth
                                required
                                value={appFormData.name}
                                onChange={(e) => handleAppFormChange("name", e.target.value)}
                                disabled={dialogLoading}
                            />
                            <TextField
                                label="Description"
                                fullWidth
                                multiline
                                rows={3}
                                value={appFormData.description}
                                onChange={(e) => handleAppFormChange("description", e.target.value)}
                                disabled={dialogLoading}
                            />
                            <TextField
                                label="Icon URL"
                                fullWidth
                                value={appFormData.iconUrl}
                                onChange={(e) => handleAppFormChange("iconUrl", e.target.value)}
                                disabled={dialogLoading}
                                helperText="URL to application icon/logo"
                            />
                            <FormControl fullWidth required disabled={dialogLoading}>
                                <InputLabel>Category</InputLabel>
                                <Select
                                    label="Category"
                                    value={appFormData.categoryId}
                                    onChange={(e) => handleAppFormChange("categoryId", e.target.value)}
                                >
                                    <MenuItem value={0} disabled>
                                        <em>Select a category</em>
                                    </MenuItem>
                                    {categories.map((category) => (
                                        <MenuItem key={category.id} value={category.id}>
                                            {category.displayName}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        </Box>
                    </TabPanel>

                    <TabPanel value={tabValue} index={1}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            {dialogMode === "create" && (
                                <Alert severity="info" sx={{ mb: 1 }}>
                                    Fill out this section to create an initial manifest for the application. This is optional and can be done later.
                                </Alert>
                            )}
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Version"
                                        fullWidth
                                        value={manifestFormData.Version}
                                        onChange={(e) => handleManifestFormChange("Version", e.target.value)}
                                        disabled={dialogLoading}
                                        helperText="e.g., 1.0.0"
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Binary Version"
                                        fullWidth
                                        value={manifestFormData.BinaryVersion}
                                        onChange={(e) => handleManifestFormChange("BinaryVersion", e.target.value)}
                                        disabled={dialogLoading}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <TextField
                                        label="Binary Package"
                                        fullWidth
                                        value={manifestFormData.BinaryPackage}
                                        onChange={(e) => handleManifestFormChange("BinaryPackage", e.target.value)}
                                        disabled={dialogLoading}
                                        helperText="Package identifier or URL"
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Config Version"
                                        fullWidth
                                        value={manifestFormData.ConfigVersion}
                                        onChange={(e) => handleManifestFormChange("ConfigVersion", e.target.value)}
                                        disabled={dialogLoading}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Config Package"
                                        fullWidth
                                        value={manifestFormData.ConfigPackage}
                                        onChange={(e) => handleManifestFormChange("ConfigPackage", e.target.value)}
                                        disabled={dialogLoading}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <FormControl fullWidth disabled={dialogLoading}>
                                        <InputLabel>Config Merge Strategy</InputLabel>
                                        <Select
                                            label="Config Merge Strategy"
                                            value={manifestFormData.ConfigMergeStrategy}
                                            onChange={(e) => handleManifestFormChange("ConfigMergeStrategy", e.target.value)}
                                        >
                                            <MenuItem value="Replace">Replace</MenuItem>
                                            <MenuItem value="Merge">Merge</MenuItem>
                                            <MenuItem value="KeepExisting">Keep Existing</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <FormControl fullWidth disabled={dialogLoading}>
                                        <InputLabel>Update Type</InputLabel>
                                        <Select
                                            label="Update Type"
                                            value={manifestFormData.UpdateType}
                                            onChange={(e) => handleManifestFormChange("UpdateType", e.target.value)}
                                        >
                                            <MenuItem value="Optional">Optional</MenuItem>
                                            <MenuItem value="Recommended">Recommended</MenuItem>
                                            <MenuItem value="Required">Required</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <TextField
                                        label="Release Notes"
                                        fullWidth
                                        multiline
                                        rows={3}
                                        value={manifestFormData.ReleaseNotes}
                                        onChange={(e) => handleManifestFormChange("ReleaseNotes", e.target.value)}
                                        disabled={dialogLoading}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Published At"
                                        type="datetime-local"
                                        fullWidth
                                        value={manifestFormData.PublishedAt}
                                        onChange={(e) => handleManifestFormChange("PublishedAt", e.target.value)}
                                        disabled={dialogLoading}
                                        InputLabelProps={{ shrink: true }}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Box sx={{ display: 'flex', gap: 2, mt: 1 }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={manifestFormData.ForceUpdate}
                                                    onChange={(e) => handleManifestFormChange("ForceUpdate", e.target.checked)}
                                                    disabled={dialogLoading}
                                                />
                                            }
                                            label="Force Update"
                                        />
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={manifestFormData.IsStable}
                                                    onChange={(e) => handleManifestFormChange("IsStable", e.target.checked)}
                                                    disabled={dialogLoading}
                                                />
                                            }
                                            label="Is Stable"
                                        />
                                    </Box>
                                </Grid>
                            </Grid>
                        </Box>
                    </TabPanel>

                    <TabPanel value={tabValue} index={2}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            <Alert severity="info" sx={{ mb: 1 }}>
                                Upload a package file for this application. This is optional.
                            </Alert>
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Package Version"
                                        fullWidth
                                        value={packageFormData.Version}
                                        onChange={(e) => handlePackageFormChange("Version", e.target.value)}
                                        disabled={dialogLoading}
                                        helperText="e.g., 1.0.0"
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <FormControl fullWidth disabled={dialogLoading}>
                                        <InputLabel>Package Type</InputLabel>
                                        <Select
                                            label="Package Type"
                                            value={packageFormData.PackageType}
                                            onChange={(e) => handlePackageFormChange("PackageType", e.target.value as string)}
                                        >
                                            <MenuItem value="Binary">Binary</MenuItem>
                                            <MenuItem value="Config">Config</MenuItem>
                                            <MenuItem value="Full">Full Package</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Button
                                        variant="outlined"
                                        component="label"
                                        fullWidth
                                        startIcon={<AttachFileIcon />}
                                        disabled={dialogLoading}
                                        sx={{ py: 2 }}
                                    >
                                        {selectedFile ? selectedFile.name : "Choose Package File"}
                                        <input
                                            type="file"
                                            hidden
                                            onChange={handleFileChange}
                                            accept=".zip,.rar,.7z,.exe,.msi"
                                        />
                                    </Button>
                                    {selectedFile && (
                                        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                                            File size: {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                                        </Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <TextField
                                        label="Release Notes"
                                        fullWidth
                                        multiline
                                        rows={3}
                                        value={packageFormData.ReleaseNotes}
                                        onChange={(e) => handlePackageFormChange("ReleaseNotes", e.target.value)}
                                        disabled={dialogLoading}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Minimum Client Version"
                                        fullWidth
                                        value={packageFormData.MinimumClientVersion}
                                        onChange={(e) => handlePackageFormChange("MinimumClientVersion", e.target.value)}
                                        disabled={dialogLoading}
                                        helperText="Minimum version required to install"
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, mt: 1 }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={packageFormData.IsStable}
                                                    onChange={(e) => handlePackageFormChange("IsStable", e.target.checked)}
                                                    disabled={dialogLoading}
                                                />
                                            }
                                            label="Is Stable Release"
                                        />
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={packageFormData.PublishImmediately}
                                                    onChange={(e) => handlePackageFormChange("PublishImmediately", e.target.checked)}
                                                    disabled={dialogLoading}
                                                />
                                            }
                                            label="Publish Immediately"
                                        />
                                    </Box>
                                </Grid>
                            </Grid>
                        </Box>
                    </TabPanel>
                </DialogContent>
                <DialogActions>
                    <Button
                        onClick={handleCloseDialog}
                        disabled={dialogLoading}
                        sx={{ border: 1 }}
                    >
                        Cancel
                    </Button>
                    <Button
                        onClick={handleSubmit}
                        variant="contained"
                        disabled={dialogLoading}
                        startIcon={<SaveIcon />}
                    >
                        {dialogLoading ? "Saving..." : "Save"}
                    </Button>
                </DialogActions>
            </Dialog>

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
                        Are you sure you want to delete the application <strong>"{deletingApplication?.name}"</strong>?
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
                        onClick={handleDelete}
                        variant="contained"
                        color="error"
                        disabled={deleteLoading}
                        startIcon={<DeleteIcon />}
                    >
                        {deleteLoading ? "Deleting..." : "Delete"}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* View Manifest Dialog */}
            <Dialog
                open={viewManifestDialogOpen}
                onClose={handleCloseViewManifestDialog}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle>
                    Application Manifest
                    {!editingManifest && viewingManifest && (
                        <IconButton
                            onClick={handleEditManifest}
                            sx={{ float: 'right' }}
                            color="primary"
                        >
                            <EditIcon />
                        </IconButton>
                    )}
                </DialogTitle>
                <DialogContent>
                    {manifestLoading ? (
                        <Box sx={{ py: 4 }}>
                            <LinearProgress />
                            <Typography sx={{ mt: 2, textAlign: 'center' }}>Loading manifest...</Typography>
                        </Box>
                    ) : viewingManifest ? (
                        <Box sx={{ mt: 2 }}>
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Version</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            size="small"
                                            value={viewingManifest.version}
                                            onChange={(e) => handleManifestFieldChange('version', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.version}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Binary Version</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            size="small"
                                            value={viewingManifest.binaryVersion}
                                            onChange={(e) => handleManifestFieldChange('binaryVersion', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.binaryVersion}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Typography variant="body2" color="text.secondary">Binary Package</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            size="small"
                                            value={viewingManifest.binaryPackage}
                                            onChange={(e) => handleManifestFieldChange('binaryPackage', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.binaryPackage}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Config Version</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            size="small"
                                            value={viewingManifest.configVersion || ''}
                                            onChange={(e) => handleManifestFieldChange('configVersion', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.configVersion || 'N/A'}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Config Package</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            size="small"
                                            value={viewingManifest.configPackage || ''}
                                            onChange={(e) => handleManifestFieldChange('configPackage', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.configPackage || 'N/A'}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Config Merge Strategy</Typography>
                                    {editingManifest ? (
                                        <FormControl fullWidth size="small">
                                            <Select
                                                value={viewingManifest.configMergeStrategy}
                                                onChange={(e) => handleManifestFieldChange('configMergeStrategy', e.target.value)}
                                            >
                                                <MenuItem value="Replace">Replace</MenuItem>
                                                <MenuItem value="Merge">Merge</MenuItem>
                                                <MenuItem value="KeepExisting">Keep Existing</MenuItem>
                                            </Select>
                                        </FormControl>
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.configMergeStrategy}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Update Type</Typography>
                                    {editingManifest ? (
                                        <FormControl fullWidth size="small">
                                            <Select
                                                value={viewingManifest.updateType}
                                                onChange={(e) => handleManifestFieldChange('updateType', e.target.value)}
                                            >
                                                <MenuItem value="Optional">Optional</MenuItem>
                                                <MenuItem value="Recommended">Recommended</MenuItem>
                                                <MenuItem value="Required">Required</MenuItem>
                                            </Select>
                                        </FormControl>
                                    ) : (
                                        <Chip label={viewingManifest.updateType} color="primary" size="small" />
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Typography variant="body2" color="text.secondary">Release Notes</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            multiline
                                            rows={3}
                                            value={viewingManifest.releaseNotes || ''}
                                            onChange={(e) => handleManifestFieldChange('releaseNotes', e.target.value)}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.releaseNotes || 'N/A'}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 4 }}>
                                    <Typography variant="body2" color="text.secondary">Force Update</Typography>
                                    {editingManifest ? (
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={viewingManifest.forceUpdate}
                                                    onChange={(e) => handleManifestFieldChange('forceUpdate', e.target.checked)}
                                                />
                                            }
                                            label="Force Update"
                                        />
                                    ) : (
                                        <Chip label={viewingManifest.forceUpdate ? "Yes" : "No"} color={viewingManifest.forceUpdate ? "error" : "default"} size="small" />
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 4 }}>
                                    <Typography variant="body2" color="text.secondary">Is Stable</Typography>
                                    {editingManifest ? (
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={viewingManifest.isStable}
                                                    onChange={(e) => handleManifestFieldChange('isStable', e.target.checked)}
                                                />
                                            }
                                            label="Is Stable"
                                        />
                                    ) : (
                                        <Chip label={viewingManifest.isStable ? "Yes" : "No"} color={viewingManifest.isStable ? "success" : "warning"} size="small" />
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 4 }}>
                                    <Typography variant="body2" color="text.secondary">Status</Typography>
                                    <Chip label={viewingManifest.isActive ? "Active" : "Inactive"} color={viewingManifest.isActive ? "success" : "error"} size="small" />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Published At</Typography>
                                    {editingManifest ? (
                                        <TextField
                                            fullWidth
                                            type="datetime-local"
                                            size="small"
                                            value={new Date(viewingManifest.publishedAt).toISOString().slice(0, 16)}
                                            onChange={(e) => handleManifestFieldChange('publishedAt', e.target.value)}
                                            InputLabelProps={{ shrink: true }}
                                        />
                                    ) : (
                                        <Typography variant="body1" fontWeight={500}>{FormatUtcTime.formatDateTime(viewingManifest.publishedAt)}</Typography>
                                    )}
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Updated At</Typography>
                                    <Typography variant="body1" fontWeight={500}>{FormatUtcTime.formatDateTime(viewingManifest.updatedAt)}</Typography>
                                </Grid>
                            </Grid>
                        </Box>
                    ) : (
                        <Alert severity="info">No manifest available for this application</Alert>
                    )}
                </DialogContent>
                <DialogActions>
                    {editingManifest ? (
                        <>
                            <Button onClick={() => setEditingManifest(false)}>Cancel</Button>
                            <Button
                                onClick={handleSaveManifest}
                                variant="contained"
                                startIcon={<SaveIcon />}
                                disabled={manifestLoading}
                            >
                                {manifestLoading ? "Saving..." : "Save Changes"}
                            </Button>
                        </>
                    ) : (
                        <Button onClick={handleCloseViewManifestDialog}>Close</Button>
                    )}
                </DialogActions>
            </Dialog>

            {/* Snackbar for notifications */}
            <Snackbar
                open={snackbar.open}
                autoHideDuration={6000}
                onClose={handleCloseSnackbar}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            >
                <Alert onClose={handleCloseSnackbar} severity={snackbar.severity} sx={{ width: '100%' }}>
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </AdminLayout>
    );
}
