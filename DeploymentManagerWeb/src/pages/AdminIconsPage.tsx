import {
    Box,
    Card,
    CardContent,
    Typography,
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
    Pagination,
    IconButton,
    Tooltip,
    Snackbar,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Chip,
    Switch,
    FormControlLabel,
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    Add as AddIcon,
    Edit as EditIcon,
    Delete as DeleteIcon,
    Save as SaveIcon,
    FilterListOff as FilterListOffIcon,
    Image as ImageIcon,
    FilterList as FilterListIcon
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { iconService } from "../services/deploymentManagerService";
import type { IconResponse } from "../type/iconType";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { extractErrorMessage } from "../utils/errorHandler";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";

export default function AdminIconsPage() {
    useSetPageTitle(PAGE_TITLES.ICONS);
    const API_BASE = (window as any)._env_?.API_BASE;
    const [icons, setIcons] = useState<IconResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [filterType, setFilterType] = useState<number | "">("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [snackbar, setSnackbar] = useState({
        open: false,
        message: "",
        severity: "success" as "success" | "error" | "warning"
    });

    // Dialog states
    const [dialogOpen, setDialogOpen] = useState(false);
    const [dialogMode, setDialogMode] = useState<"create" | "edit">("create");
    const [dialogLoading, setDialogLoading] = useState(false);
    const [editingIcon, setEditingIcon] = useState<IconResponse | null>(null);

    // Delete dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingIcon, setDeletingIcon] = useState<IconResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    // Form states
    const [formData, setFormData] = useState({
        name: "",
        type: 1,
        description: "",
        isActive: true,
    });
    const [formErrors, setFormErrors] = useState({
        name: false,
        type: false,
        file: false,
    });
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string>("");

    // Icon types
    const iconTypes = [
        { value: 1, label: "Application" },
        { value: 2, label: "Category" },
    ];

    // Load icons
    const loadIcons = async () => {
        setLoading(true);
        try {
            const data = await iconService.getAllIcons();
            setIcons(data);
            setError(null);
        } catch (error: any) {
            console.error("Error loading icons:", error);
            const errorMessage = extractErrorMessage(error, "Failed to load icons");
            setError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadIcons();
    }, []);

    // Filtering logic
    const filteredIcons = useMemo(() => {
        return icons.filter(icon => {
            const searchMatch = !searchTerm ||
                icon.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                icon.type.toString().toLowerCase().includes(searchTerm.toLowerCase()) ||
                icon.description?.toLowerCase().includes(searchTerm.toLowerCase());

            const typeMatch = !filterType || icon.type === filterType;

            return searchMatch && typeMatch;
        });
    }, [icons, searchTerm, filterType]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredIcons.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredIcons.slice(startIndex, endIndex);

    // Reset to first page when filters change
    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, itemsPerPage, filterType]);

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(event.target.value);
    };

    const handleItemsPerPageChange = (event: any) => {
        setItemsPerPage(event.target.value);
        setCurrentPage(1);
    };

    const handleClearFilters = () => {
        setSearchTerm("");
        setFilterType("");
        setCurrentPage(1);
    };

    const showSnackbar = (message: string, severity: "success" | "error" | "warning") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    // Dialog handlers
    const handleOpenCreateDialog = () => {
        setDialogMode("create");
        setEditingIcon(null);
        setFormData({
            name: "",
            type: 1,
            description: "",
            isActive: true,
        });
        setSelectedFile(null);
        setPreviewUrl("");
        setFormErrors({ name: false, type: false, file: false });
        setDialogOpen(true);
    };

    const handleOpenEditDialog = (icon: IconResponse) => {
        setDialogMode("edit");
        setEditingIcon(icon);
        setFormData({
            name: icon.name,
            type: icon.type,
            description: icon.description || "",
            isActive: icon.isActive,
        });
        setSelectedFile(null);
        setPreviewUrl(icon.fileUrl);
        setFormErrors({ name: false, type: false, file: false });
        setDialogOpen(true);
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingIcon(null);
        setSelectedFile(null);
        setPreviewUrl("");
    };

    const handleFormChange = (field: string, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (formErrors[field as keyof typeof formErrors]) {
            setFormErrors(prev => ({ ...prev, [field]: false }));
        }
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            setSelectedFile(file);
            const reader = new FileReader();
            reader.onloadend = () => {
                setPreviewUrl(reader.result as string);
            };
            reader.readAsDataURL(file);
            setFormErrors(prev => ({ ...prev, file: false }));
        }
    };

    const validateForm = (): boolean => {
        const errors = {
            name: !formData.name.trim(),
            type: !formData.type,
            file: dialogMode === "create" && !selectedFile,
        };
        setFormErrors(errors);

        if (errors.name) {
            showSnackbar("Icon name is required", "error");
            return false;
        }
        if (errors.type) {
            showSnackbar("Icon type is required", "error");
            return false;
        }
        if (errors.file) {
            showSnackbar("Icon file is required", "error");
            return false;
        }
        return true;
    };

    const handleSubmit = async () => {
        if (!validateForm()) return;

        setDialogLoading(true);
        try {
            const formDataToSend = new FormData();
            formDataToSend.append("Name", formData.name);
            formDataToSend.append("Type", formData.type.toString());
            formDataToSend.append("Description", formData.description);
            formDataToSend.append("IsActive", formData.isActive.toString());

            if (selectedFile) {
                formDataToSend.append("File", selectedFile);
            }

            if (dialogMode === "create") {
                await iconService.createIcon(formDataToSend);
                showSnackbar("Icon created successfully!", "success");
            } else if (editingIcon) {
                await iconService.updateIcon(editingIcon.id, formDataToSend);
                showSnackbar("Icon updated successfully!", "success");
            }

            handleCloseDialog();
            loadIcons();
        } catch (error: any) {
            console.error("Error saving icon:", error);
            const errorMessage = extractErrorMessage(error, "Error saving icon");
            showSnackbar(errorMessage, "error");
        } finally {
            setDialogLoading(false);
        }
    };

    // Delete handlers
    const handleOpenDeleteDialog = (icon: IconResponse) => {
        setDeletingIcon(icon);
        setDeleteDialogOpen(true);
    };

    const handleCloseDeleteDialog = () => {
        setDeleteDialogOpen(false);
        setDeletingIcon(null);
    };

    const handleDelete = async () => {
        if (!deletingIcon) return;

        setDeleteLoading(true);
        try {
            await iconService.deleteIcon(deletingIcon.id);
            showSnackbar("Icon deleted successfully!", "success");
            handleCloseDeleteDialog();
            loadIcons();
        } catch (error: any) {
            console.error("Error deleting icon:", error);
            const errorMessage = extractErrorMessage(error, "Error deleting icon");
            showSnackbar(errorMessage, "error");
        } finally {
            setDeleteLoading(false);
        }
    };

    return (
        <AdminLayout>
            <Box>
                {/* Header */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" color="text.secondary">
                        Manage application icons, create new icons, and organize icon types
                    </Typography>
                </Box>
                {/* Filters and Actions */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                            <FilterListIcon sx={{ mr: 1 }} /> Filters
                        </Typography>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'center' }}>
                            {/* Search */}
                            <TextField
                                size="small"
                                label="Search icons..."
                                value={searchTerm}
                                onChange={handleSearchChange}
                                InputProps={{
                                    startAdornment: <SearchIcon sx={{ color: 'text.secondary', mr: 1 }} />
                                }}
                                sx={{ width: 250 }}
                            />

                            {/* Type Filter */}
                            <FormControl size="small" sx={{ minWidth: 150 }}>
                                <Select
                                    value={filterType}
                                    onChange={(e) => setFilterType(e.target.value)}
                                    displayEmpty
                                >
                                    <MenuItem value="">
                                        <em>All Types</em>
                                    </MenuItem>
                                    {iconTypes.map((type) => (
                                        <MenuItem key={type.value} value={type.value}>
                                            {type.label}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>

                            {/* Items per page */}
                            <FormControl size="small" sx={{ minWidth: 120 }}>
                                <InputLabel>Items/page</InputLabel>
                                <Select
                                    value={itemsPerPage}
                                    label="Items/page"
                                    onChange={handleItemsPerPageChange}
                                >
                                    <MenuItem value={5}>5</MenuItem>
                                    <MenuItem value={10}>10</MenuItem>
                                    <MenuItem value={20}>20</MenuItem>
                                    <MenuItem value={50}>50</MenuItem>
                                </Select>
                            </FormControl>

                            {/* Clear Filters */}
                            <Tooltip title="Clear all filters">
                                <Button
                                    variant="outlined"
                                    onClick={handleClearFilters}
                                    disabled={!searchTerm && !filterType}
                                    sx={{ minWidth: 'auto', px: 2 }}
                                >
                                    <FilterListOffIcon />
                                </Button>
                            </Tooltip>

                            {/* Refresh */}
                            <Button
                                variant="contained"
                                onClick={loadIcons}
                                disabled={loading}
                                startIcon={<RefreshIcon />}
                            >
                                Refresh
                            </Button>

                            {/* Create New */}
                            <Button
                                variant="contained"
                                color="success"
                                startIcon={<AddIcon />}
                                onClick={handleOpenCreateDialog}
                            >
                                Create new
                            </Button>

                            {/* Stats */}
                            <Box sx={{ mb: 2, flexGrow: 1 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Total: {filteredIcons.length} icons
                                    {(searchTerm || filterType) && ` (filtered from ${icons.length})`}
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Active: ${icons.filter(i => i.isActive).length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Inactive: ${icons.filter(i => !i.isActive).length}`}
                                        color="error"
                                        size="small"
                                        variant="outlined"
                                    />
                                </Box>
                            </Box>
                        </Box>


                    </CardContent>
                </Card>

                {loading && <LinearProgress sx={{ mb: 2 }} />}

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                {/* Icons Table */}
                <Card>
                    <CardContent sx={{ p: 0 }}>
                        <TableContainer component={Paper} variant="outlined">
                            <Table>
                                <TableHead>
                                    <TableRow>
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
                                        <TableCell width="100px" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }} align="center">Preview</TableCell>
                                        <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>Icon Name</TableCell>
                                        <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>Icon Type</TableCell>
                                        <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>File Extension</TableCell>
                                        <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>Size</TableCell>
                                        <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>Icon Url</TableCell>
                                        <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>Status</TableCell>
                                        <TableCell sx={{ fontWeight: 500 }}>Updated At</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {currentPageData.length > 0 ? (
                                        currentPageData.map((icon) => (
                                            <TableRow key={icon.id} hover>
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
                                                    <Tooltip title="Edit">
                                                        <IconButton
                                                            size="small"
                                                            color="primary"
                                                            onClick={() => handleOpenEditDialog(icon)}
                                                            sx={{ mr: 1 }}
                                                        >
                                                            <EditIcon fontSize="small" />
                                                        </IconButton>
                                                    </Tooltip>
                                                    <Tooltip title="Delete">
                                                        <IconButton
                                                            size="small"
                                                            color="error"
                                                            onClick={() => handleOpenDeleteDialog(icon)}
                                                        >
                                                            <DeleteIcon fontSize="small" />
                                                        </IconButton>
                                                    </Tooltip>
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Box
                                                        component="img"
                                                        src={`${API_BASE}${icon.fileUrl}`}
                                                        alt={icon.name}
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
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Typography variant="body2" fontWeight={600}>
                                                        {icon.name}
                                                    </Typography>
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Chip label={icon.type === 1 ? "Application" : "Category"} size="small" color={icon.type === 1 ? "success" : "secondary"} variant="outlined" />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Chip label={icon.fileExtension} size="small" variant="outlined" color="primary" />
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {(icon.fileSize / 1024).toFixed(2)} KB
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: 200 }}>
                                                        {icon.fileUrl || "-"}
                                                    </Typography>
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Chip
                                                        label={icon.isActive ? "Active" : "Inactive"}
                                                        color={icon.isActive ? "success" : "error"}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {FormatUtcTime.formatDateTime(icon.updatedAt)}
                                                </TableCell>

                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={9} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    {searchTerm || filterType
                                                        ? `No icons found matching your filters`
                                                        : "No icons available"}
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {filteredIcons.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {filteredIcons.length > 0 ? startIndex + 1 : 0}-{Math.min(endIndex, filteredIcons.length)} of {filteredIcons.length} items
                                </Typography>
                                {filteredIcons.length > itemsPerPage && (
                                    <Pagination
                                        count={totalPages}
                                        page={currentPage}
                                        onChange={(_, page) => setCurrentPage(page)}
                                        color="primary"
                                        showFirstButton
                                        showLastButton
                                    />
                                )}
                            </Box>
                        )}
                    </CardContent>
                </Card>

                {/* Create/Edit Dialog */}
                <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
                    <DialogTitle>
                        {dialogMode === "create" ? "Create New Icon" : "Edit Icon"}
                    </DialogTitle>
                    <DialogContent>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 2 }}>
                            <TextField
                                label="Icon Name"
                                fullWidth
                                required
                                value={formData.name}
                                onChange={(e) => handleFormChange("name", e.target.value)}
                                error={formErrors.name}
                                helperText={formErrors.name ? "Name is required" : ""}
                                disabled={dialogLoading}
                            />

                            <FormControl fullWidth required error={formErrors.type}>
                                <InputLabel>Type</InputLabel>
                                <Select
                                    value={formData.type}
                                    label="Type"
                                    onChange={(e) => handleFormChange("type", e.target.value)}
                                    disabled={dialogLoading}
                                >
                                    {iconTypes.map((type) => (
                                        <MenuItem key={type.value} value={type.value}>
                                            {type.label}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>

                            {/* <TextField
                                label="Description"
                                fullWidth
                                multiline
                                rows={3}
                                value={formData.description}
                                onChange={(e) => handleFormChange("description", e.target.value)}
                                disabled={dialogLoading}
                            /> */}

                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={formData.isActive}
                                        onChange={(e) => handleFormChange("isActive", e.target.checked)}
                                        disabled={dialogLoading}
                                    />
                                }
                                label="Active"
                            />

                            <Box>
                                <Button
                                    variant="outlined"
                                    component="label"
                                    fullWidth
                                    startIcon={<ImageIcon />}
                                    disabled={dialogLoading}
                                >
                                    {selectedFile ? selectedFile.name : dialogMode === "edit" ? "Change Icon File" : "Upload Icon File"}
                                    <input
                                        type="file"
                                        hidden
                                        accept=".ico"
                                        onChange={handleFileChange}
                                    />
                                </Button>
                                {formErrors.file && (
                                    <Typography variant="caption" color="error" sx={{ mt: 0.5, display: 'block' }}>
                                        Icon file is required
                                    </Typography>
                                )}
                            </Box>

                            {previewUrl && (
                                <Box sx={{ textAlign: 'center', mt: 2 }}>
                                    <Typography variant="body2" color="text.secondary" gutterBottom>
                                        Preview:
                                    </Typography>
                                    <Box
                                        component="img"
                                        src={selectedFile ? previewUrl : `${API_BASE}${previewUrl}`}
                                        alt="Preview"
                                        sx={{
                                            maxWidth: 200,
                                            maxHeight: 200,
                                            objectFit: 'contain',
                                            border: '2px solid #e0e0e0',
                                            borderRadius: 2,
                                            p: 2,
                                        }}
                                        onError={(_e: any) => {
                                            console.error('Failed to load image:', previewUrl);
                                        }}
                                    />
                                </Box>
                            )}
                        </Box>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleCloseDialog} disabled={dialogLoading}>
                            Cancel
                        </Button>
                        <Button
                            variant="contained"
                            onClick={handleSubmit}
                            disabled={dialogLoading}
                            startIcon={<SaveIcon />}
                        >
                            {dialogLoading ? "Saving..." : "Save"}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Delete Confirmation Dialog */}
                <Dialog open={deleteDialogOpen} onClose={handleCloseDeleteDialog} maxWidth="sm" fullWidth>
                    <DialogTitle>Confirm Delete</DialogTitle>
                    <DialogContent>
                        <Typography>
                            Are you sure you want to delete the icon <strong>"{deletingIcon?.name}"</strong>?
                        </Typography>
                        <Alert severity="warning" sx={{ mt: 2 }}>
                            This action cannot be undone.
                        </Alert>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleCloseDeleteDialog} disabled={deleteLoading}>
                            Cancel
                        </Button>
                        <Button
                            variant="contained"
                            color="error"
                            onClick={handleDelete}
                            disabled={deleteLoading}
                            startIcon={<DeleteIcon />}
                        >
                            {deleteLoading ? "Deleting..." : "Delete"}
                        </Button>
                    </DialogActions>
                </Dialog>

                {/* Snackbar */}
                <Snackbar
                    open={snackbar.open}
                    autoHideDuration={4000}
                    onClose={handleCloseSnackbar}
                    anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
                >
                    <Alert
                        onClose={handleCloseSnackbar}
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