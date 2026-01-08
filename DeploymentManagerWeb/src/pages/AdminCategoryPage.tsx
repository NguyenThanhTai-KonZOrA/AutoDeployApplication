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
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    Add as AddIcon,
    Edit as EditIcon,
    Delete as DeleteIcon,
    Save as SaveIcon,
    FilterList as FilterListIcon
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { categoryService, iconService } from "../services/deploymentManagerService";
import type { CategoryCreateOrUpdateRequest, CategoryResponse } from "../type/categoryType";
import type { IconResponse } from "../type/iconType";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

export default function AdminCategoryPage() {
    useSetPageTitle(PAGE_TITLES.CATEGORIES);
    const API_BASE = (window as any)._env_?.API_BASE;
    const [categories, setCategories] = useState<CategoryResponse[]>([]);
    const [icons, setIcons] = useState<IconResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" as "success" | "error" });

    // Dialog states
    const [dialogOpen, setDialogOpen] = useState(false);
    const [dialogMode, setDialogMode] = useState<"create" | "edit">("create");
    const [dialogLoading, setDialogLoading] = useState(false);
    const [editingCategory, setEditingCategory] = useState<CategoryResponse | null>(null);

    // Form states
    const [formData, setFormData] = useState<CategoryCreateOrUpdateRequest>({
        name: "",
        displayName: "",
        description: "",
        icon: "",
        displayOrder: 0,
        iconUrl: "",
    });

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingCategory, setDeletingCategory] = useState<CategoryResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    const showSnackbar = (message: string, severity: "success" | "error") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadCategories = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await categoryService.getAllCategories();
            setCategories(data);
            await loadIcons();
        } catch (error: any) {
            console.error("Error loading categories:", error);
            let errorMessage = "Failed to load categories data";
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

    const loadIcons = async () => {
        try {
            const data = await iconService.getIconByType(2); // Type 2 for Category icons
            setIcons(data.filter(icon => icon.isActive)); // Only show active icons
        } catch (error: any) {
            console.error("Error loading icons:", error);
            showSnackbar("Failed to load icons", "error");
        }
    };

    const handleOpenCreateDialog = () => {
        setDialogMode("create");
        setEditingCategory(null);
        setFormData({
            name: "",
            displayName: "",
            description: "",
            icon: icons.length > 0 ? icons[0].fileUrl : "",
            displayOrder: categories.length + 1,
            iconUrl: icons.length > 0 ? icons[0].fileUrl : "",
        });
        setDialogOpen(true);
    };

    const handleOpenEditDialog = (category: CategoryResponse) => {
        setDialogMode("edit");
        setEditingCategory(category);
        setFormData({
            name: category.name,
            displayName: category.displayName,
            description: category.description || "",
            icon: category.icon,
            displayOrder: category.displayOrder,
            iconUrl: category.iconUrl,
        });
        setDialogOpen(true);
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingCategory(null);
    };

    const handleFormChange = (field: keyof CategoryCreateOrUpdateRequest, value: string | number) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleSubmit = async () => {
        // Validation
        if (!formData.name.trim()) {
            showSnackbar("Category name is required", "error");
            return;
        }

        if (!formData.displayName.trim()) {
            showSnackbar("Display name is required", "error");
            return;
        }

        if (!formData.iconUrl.trim()) {
            showSnackbar("Icon is required", "error");
            return;
        }

        if (formData.displayOrder <= 0) {
            showSnackbar("Display order must be greater than 0", "error");
            return;
        }

        try {
            setDialogLoading(true);
            const request: CategoryCreateOrUpdateRequest = {
                name: formData.name.trim(),
                displayName: formData.displayName.trim(),
                description: formData.description?.trim(),
                icon: formData.iconUrl.trim(),
                displayOrder: formData.displayOrder,
                iconUrl: formData.iconUrl.trim(),
            };

            if (dialogMode === "create") {
                const result = await categoryService.createCategory(request);
                setCategories(prev => [...prev, result]);
                showSnackbar(`Category "${result.displayName}" created successfully!`, "success");
            } else {
                if (!editingCategory) return;

                const result = await categoryService.updateCategory(editingCategory.id, request);
                setCategories(prev =>
                    prev.map(cat => cat.id === result.id ? result : cat)
                );
                showSnackbar(`Category "${result.displayName}" updated successfully!`, "success");
            }

            handleCloseDialog();
        } catch (error: any) {
            console.error("Error submitting category:", error);
            let errorMessage = dialogMode === "create"
                ? "Error creating category"
                : "Error updating category";

            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setDialogLoading(false);
            loadCategories();
        }
    };

    const handleOpenDeleteDialog = (category: CategoryResponse) => {
        setDeletingCategory(category);
        setDeleteDialogOpen(true);
    };

    const handleCloseDeleteDialog = () => {
        setDeleteDialogOpen(false);
        setDeletingCategory(null);
    };

    const handleDelete = async () => {
        if (!deletingCategory) return;

        try {
            setDeleteLoading(true);
            await categoryService.deleteCategory(deletingCategory.id);
            setCategories(prev => prev.filter(cat => cat.id !== deletingCategory.id));
            showSnackbar(`Category "${deletingCategory.displayName}" deleted successfully!`, "success");
            handleCloseDeleteDialog();
        } catch (error: any) {
            console.error("Error deleting category:", error);
            let errorMessage = "Error deleting category";

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

    // Auto load data on component mount
    useEffect(() => {
        loadCategories();
    }, []);

    // Search and pagination logic
    const filteredCategories = useMemo(() => {
        if (!searchTerm) return categories;

        return categories.filter(cat =>
            cat.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            cat.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
            (cat.description && cat.description.toLowerCase().includes(searchTerm.toLowerCase()))
        );
    }, [categories, searchTerm]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredCategories.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredCategories.slice(startIndex, endIndex);

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
                        Manage application categories, create new categories, and organize applications
                    </Typography>
                </Box>

                {/* Search and Actions */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                            <FilterListIcon sx={{ mr: 1 }} /> Filters
                        </Typography>
                        <Grid container spacing={3} alignItems="center">
                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="Search categories..."
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
                                    onClick={loadCategories}
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
                                    Total: {filteredCategories.length} categories
                                    {searchTerm && ` (filtered from ${categories.length})`}
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Active: ${categories.filter(cat => cat.isActive).length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Inactive: ${categories.filter(cat => !cat.isActive).length}`}
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
                        <IconButton size="small" onClick={loadCategories}>
                            <RefreshIcon />
                        </IconButton>
                    }>
                        {error}
                    </Alert>
                )}

                {/* Categories Table */}
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
                                        {/* <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 80 }}>
                                            ID
                                        </TableCell> */}
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Icon
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 150 }}>
                                            Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Display Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 250 }}>
                                            Description
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Display Order
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Applications
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
                                                <TableCell align="center"><Skeleton width="120px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="40px" /></TableCell>
                                                <TableCell><Skeleton width="60px" /></TableCell>
                                                <TableCell><Skeleton width="120px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="200px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                            </TableRow>
                                        ))
                                    ) : currentPageData.length > 0 ? (
                                        currentPageData.map((category) => (
                                            <TableRow
                                                key={category.id}
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
                                                        <Tooltip title="Edit category">
                                                            <IconButton
                                                                size="small"
                                                                color="primary"
                                                                onClick={() => handleOpenEditDialog(category)}
                                                            >
                                                                <EditIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                        <Tooltip title="Delete category">
                                                            <IconButton
                                                                size="small"
                                                                color="error"
                                                                onClick={() => handleOpenDeleteDialog(category)}
                                                            >
                                                                <DeleteIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </Box>
                                                </TableCell>
                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {category.id}
                                                </TableCell> */}
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
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
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {category.name}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {category.displayName}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {category.description || '-'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={category.displayOrder}
                                                        color="info"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={category.applicationCount}
                                                        color="primary"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={category.isActive ? "Active" : "Inactive"}
                                                        color={category.isActive ? "success" : "error"}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(category.updatedAt)}
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={10} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    {searchTerm ? `No categories found matching "${searchTerm}"` : "No categories available"}
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {filteredCategories.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {filteredCategories.length > 0 ? startIndex + 1 : 0}-{Math.min(endIndex, filteredCategories.length)} of {filteredCategories.length} items
                                </Typography>
                                {filteredCategories.length > itemsPerPage && (
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

            {/* Create/Edit Dialog */}
            <Dialog
                open={dialogOpen}
                onClose={handleCloseDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    {dialogMode === "create" ? "Create New Category" : "Edit Category"}
                </DialogTitle>
                <DialogContent>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
                        <TextField
                            label="Category Name"
                            fullWidth
                            required
                            value={formData.name}
                            onChange={(e) => handleFormChange("name", e.target.value)}
                            disabled={dialogLoading}
                            autoFocus
                            helperText="Internal name (lowercase, no spaces)"
                        />
                        <TextField
                            label="Display Name"
                            fullWidth
                            required
                            value={formData.displayName}
                            onChange={(e) => handleFormChange("displayName", e.target.value)}
                            disabled={dialogLoading}
                            helperText="User-friendly name shown in UI"
                        />
                        <TextField
                            label="Description"
                            fullWidth
                            multiline
                            rows={3}
                            value={formData.description}
                            onChange={(e) => handleFormChange("description", e.target.value)}
                            disabled={dialogLoading}
                        />
                        <FormControl fullWidth required disabled={dialogLoading}>
                            <InputLabel>Icon</InputLabel>
                            <Select
                                value={formData.iconUrl}
                                label="Icon"
                                onChange={(e) => handleFormChange("iconUrl", e.target.value)}
                                renderValue={(selected) => {
                                    const API_BASE = (window as any)._env_?.API_BASE;
                                    const selectedIcon = icons.find(icon => icon.fileUrl === selected);
                                    return (
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                            <img
                                                src={`${API_BASE}${selected}`}
                                                alt={selectedIcon?.name || selected}
                                                style={{ width: 20, height: 20 }}
                                                onError={(e) => {
                                                    (e.target as HTMLImageElement).style.display = 'none';
                                                }}
                                            />
                                            <Typography>{selectedIcon?.name || selected}</Typography>
                                        </Box>
                                    );
                                }}
                            >
                                {icons.map((icon) => {
                                    const API_BASE = (window as any)._env_?.API_BASE;
                                    return (
                                        <MenuItem key={icon.id} value={icon.fileUrl}>
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <img
                                                    src={`${API_BASE}${icon.fileUrl}`}
                                                    alt={icon.name}
                                                    style={{ width: 20, height: 20 }}
                                                    onError={(e) => {
                                                        (e.target as HTMLImageElement).style.display = 'none';
                                                    }}
                                                />
                                                <Typography>{icon.name}</Typography>
                                            </Box>
                                        </MenuItem>
                                    );
                                })}
                            </Select>
                        </FormControl>
                        <TextField
                            label="Display Order"
                            fullWidth
                            required
                            type="number"
                            value={formData.displayOrder}
                            onChange={(e) => handleFormChange("displayOrder", parseInt(e.target.value) || 0)}
                            disabled={dialogLoading}
                            helperText="Lower numbers appear first"
                        />
                    </Box>
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
                        Are you sure you want to delete the category <strong>"{deletingCategory?.displayName}"</strong>?
                    </Typography>
                    {deletingCategory && deletingCategory.applicationCount > 0 && (
                        <Alert severity="warning" sx={{ mt: 2 }}>
                            This category has {deletingCategory.applicationCount} application(s) assigned.
                            Please reassign those applications before deleting this category.
                        </Alert>
                    )}
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
                        disabled={deleteLoading || (deletingCategory?.applicationCount ?? 0) > 0}
                        startIcon={<DeleteIcon />}
                    >
                        {deleteLoading ? "Deleting..." : "Delete"}
                    </Button>
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
