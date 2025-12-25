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
    Close as CloseIcon,
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { serviceTypeService } from "../services/queueService";
import type { ServiceTypeResponse, CreateServiceTypeRequest, UpdateServiceTypeRequest, SummaryWorkFlowServiceResponse } from "../type";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

export default function AdminServiceTypePage() {
    useSetPageTitle(PAGE_TITLES.SERVICE_TYPES);
    const [serviceTypes, setServiceTypes] = useState<ServiceTypeResponse[]>([]);
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
    const [editingServiceType, setEditingServiceType] = useState<ServiceTypeResponse | null>(null);

    // Form states
    const [formData, setFormData] = useState({
        name: "",
        description: "",
        workFlowServiceId: 0,
    });

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingServiceType, setDeletingServiceType] = useState<ServiceTypeResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    // Workflow services states
    const [workflowServices, setWorkflowServices] = useState<SummaryWorkFlowServiceResponse[]>([]);
    const [workflowLoading, setWorkflowLoading] = useState(false);

    const showSnackbar = (message: string, severity: "success" | "error") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadServiceTypes = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await serviceTypeService.getAllServiceTypes();
            setServiceTypes(data);
        } catch (error: any) {
            console.error("Error loading service types:", error);
            let errorMessage = "Failed to load service types data";
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

    const loadWorkflowServices = async () => {
        try {
            setWorkflowLoading(true);
            const data = await serviceTypeService.getWorkFlowServices();
            setWorkflowServices(data);
        } catch (error: any) {
            console.error("Error loading workflow services:", error);
            let errorMessage = "Failed to load workflow services";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setWorkflowLoading(false);
        }
    };

    const handleToggleStatus = async (serviceType: ServiceTypeResponse) => {
        try {
            setToggleLoading(serviceType.id);

            if (serviceType.countersCount > 0 && serviceType.isActive) {
                showSnackbar(`Cannot deactivate "${serviceType.name}" because it is assigned to ${serviceType.countersCount} counter(s). Please reassign or remove those counters first.`, "error");
                return;
            }

            const result = await serviceTypeService.changeStatusServiceType(serviceType.id);

            if (result) {
                showSnackbar(`Changed status for "${serviceType.name}" successfully!`, "success");
                setServiceTypes(prevServiceTypes =>
                    prevServiceTypes.map(st =>
                        st.id === serviceType.id
                            ? { ...st, isActive: !st.isActive }
                            : st
                    )
                );
            } else {
                setError("Failed to change service type status");
            }
        } catch (error: any) {
            console.error("Error updating service type status:", error);
            let errorMessage = "Error updating service type status";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setToggleLoading(null);
        }
    };

    const handleOpenCreateDialog = () => {
        setDialogMode("create");
        setEditingServiceType(null);
        setFormData({
            name: "",
            description: "",
            workFlowServiceId: 0,
        });
        setDialogOpen(true);
        loadWorkflowServices(); // Load workflow services when opening dialog
    };

    const handleOpenEditDialog = (serviceType: ServiceTypeResponse) => {
        setDialogMode("edit");
        setEditingServiceType(serviceType);
        setFormData({
            name: serviceType.name,
            description: serviceType.description,
            workFlowServiceId: serviceType.workFlowServiceId || 0,
        });
        setDialogOpen(true);
        loadWorkflowServices(); // Load workflow services when opening dialog
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingServiceType(null);
        setFormData({
            name: "",
            description: "",
            workFlowServiceId: 0,
        });
    };

    const handleFormChange = (field: string, value: string | number) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleSubmit = async () => {
        // Validation
        if (!formData.name.trim()) {
            showSnackbar("Service type name is required", "error");
            return;
        }

        if (!formData.description.trim()) {
            showSnackbar("Service type description is required", "error");
            return;
        }

        if (!formData.workFlowServiceId || formData.workFlowServiceId === 0) {
            showSnackbar("Workflow service is required", "error");
            return;
        }

        try {
            setDialogLoading(true);

            if (dialogMode === "create") {
                const request: CreateServiceTypeRequest = {
                    name: formData.name.trim(),
                    description: formData.description.trim(),
                    workFlowServiceId: formData.workFlowServiceId,
                };
                const result = await serviceTypeService.createServiceType(request);
                setServiceTypes(prev => [...prev, result]);
                showSnackbar(`Service type "${result.name}" created successfully!`, "success");
            } else {
                if (!editingServiceType) return;

                const request: UpdateServiceTypeRequest = {
                    id: editingServiceType.id,
                    name: formData.name.trim(),
                    description: formData.description.trim(),
                    isActive: editingServiceType.isActive,
                    workFlowServiceId: formData.workFlowServiceId,
                };
                const result = await serviceTypeService.updateServiceType(request);
                setServiceTypes(prev =>
                    prev.map(st => st.id === result.id ? result : st)
                );
                showSnackbar(`Service type "${result.name}" updated successfully!`, "success");
            }

            handleCloseDialog();
        } catch (error: any) {
            console.error("Error submitting service type:", error);
            let errorMessage = dialogMode === "create"
                ? "Error creating service type"
                : "Error updating service type";

            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setDialogLoading(false);
        }
    };

    const handleOpenDeleteDialog = (serviceType: ServiceTypeResponse) => {
        setDeletingServiceType(serviceType);
        setDeleteDialogOpen(true);
    };

    const handleCloseDeleteDialog = () => {
        setDeleteDialogOpen(false);
        setDeletingServiceType(null);
    };

    const handleDelete = async () => {
        if (!deletingServiceType) return;

        try {
            setDeleteLoading(true);
            const result = await serviceTypeService.deleteServiceType(deletingServiceType.id);

            if (result) {
                setServiceTypes(prev => prev.filter(st => st.id !== deletingServiceType.id));
                showSnackbar(`Service type "${deletingServiceType.name}" deleted successfully!`, "success");
                handleCloseDeleteDialog();
            }
        } catch (error: any) {
            console.error("Error deleting service type:", error);
            let errorMessage = "Error deleting service type";

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
        loadServiceTypes();
    }, []);

    // Search and pagination logic
    const filteredServiceTypes = useMemo(() => {
        if (!searchTerm) return serviceTypes;

        return serviceTypes.filter(st =>
            st.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
            st.description.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }, [serviceTypes, searchTerm]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredServiceTypes.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredServiceTypes.slice(startIndex, endIndex);

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
                    {/* <Typography variant="h4" fontWeight={600} sx={{ mb: 1 }}>
                        Service Type Management
                    </Typography> */}
                    <Typography variant="body1" color="text.secondary">
                        Manage service types, create new types, update information, and control status
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
                                    label="Search service types..."
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
                                    onClick={loadServiceTypes}
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
                                    Total: {filteredServiceTypes.length} service types
                                    {searchTerm && ` (filtered from ${serviceTypes.length})`}
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Active: ${serviceTypes.filter(st => st.isActive).length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Inactive: ${serviceTypes.filter(st => !st.isActive).length}`}
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
                        <IconButton size="small" onClick={loadServiceTypes}>
                            <RefreshIcon />
                        </IconButton>
                    }>
                        {error}
                    </Alert>
                )}

                {/* Service Types Table */}
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
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 250 }}>
                                            Description
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Counters
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Workflow Service
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Status
                                        </TableCell>
                                        {/* <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Created At
                                        </TableCell> */}
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Updated At
                                        </TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {loading ? (
                                        // Loading skeleton rows
                                        Array.from({ length: itemsPerPage }).map((_, index) => (
                                            <TableRow key={index}>
                                                <TableCell align="center"><Skeleton width="120px" /></TableCell>
                                                <TableCell><Skeleton width="40px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="200px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                            </TableRow>
                                        ))
                                    ) : currentPageData.length > 0 ? (
                                        currentPageData.map((serviceType) => (
                                            <TableRow
                                                key={serviceType.id}
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
                                                        <Tooltip title="Edit service type">
                                                            <IconButton
                                                                size="small"
                                                                color="primary"
                                                                onClick={() => handleOpenEditDialog(serviceType)}
                                                            >
                                                                <EditIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                        <Tooltip title="Delete service type">
                                                            <IconButton
                                                                size="small"
                                                                color="error"
                                                                onClick={() => handleOpenDeleteDialog(serviceType)}
                                                            >
                                                                <DeleteIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </Box>
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {serviceType.id}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {serviceType.name}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {serviceType.description}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={serviceType.countersCount}
                                                        color="primary"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {serviceType.workFlowServiceName || 'N/A'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Tooltip title={`${serviceType.isActive ? 'Deactivate' : 'Activate'} service type`}>
                                                        <span>
                                                            <Switch
                                                                checked={serviceType.isActive}
                                                                onChange={() => handleToggleStatus(serviceType)}
                                                                disabled={toggleLoading === serviceType.id}
                                                                color="success"
                                                                size="small"
                                                            />
                                                        </span>
                                                    </Tooltip>
                                                </TableCell>
                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(serviceType.createdAt)}
                                                </TableCell> */}
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(serviceType.updatedAt)} <br />
                                                    <Typography variant="caption" color="text.secondary">
                                                        by {serviceType.updatedBy}
                                                    </Typography>
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={9} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    {searchTerm ? `No service types found matching "${searchTerm}"` : "No service types available"}
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {filteredServiceTypes.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {filteredServiceTypes.length > 0 ? startIndex + 1 : 0}-{Math.min(endIndex, filteredServiceTypes.length)} of {filteredServiceTypes.length} items
                                </Typography>
                                {filteredServiceTypes.length > itemsPerPage && (
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
                    {dialogMode === "create" ? "Create New Service Type" : "Edit Service Type"}
                </DialogTitle>
                <DialogContent>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
                        <TextField
                            label="Service Type Name"
                            fullWidth
                            required
                            value={formData.name}
                            onChange={(e) => handleFormChange("name", e.target.value)}
                            disabled={dialogLoading}
                            autoFocus
                        />
                        <TextField
                            label="Description"
                            required
                            fullWidth
                            multiline
                            rows={3}
                            value={formData.description}
                            onChange={(e) => handleFormChange("description", e.target.value)}
                            disabled={dialogLoading}
                        />
                        <FormControl fullWidth required disabled={dialogLoading || workflowLoading}>
                            <InputLabel id="workflow-service-label">Workflow Service</InputLabel>
                            <Select
                                labelId="workflow-service-label"
                                label="Workflow Service"
                                value={formData.workFlowServiceId}
                                onChange={(e) => handleFormChange("workFlowServiceId", e.target.value)}
                            >
                                <MenuItem value={0} disabled>
                                    <em>Select a workflow service</em>
                                </MenuItem>
                                {workflowLoading ? (
                                    <MenuItem disabled>Loading workflow services...</MenuItem>
                                ) : workflowServices.length > 0 ? (
                                    workflowServices.map((workflow) => (
                                        <MenuItem key={workflow.id} value={workflow.id}>
                                            {workflow.name}
                                            {workflow.description && (
                                                <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                                                    - {workflow.description}
                                                </Typography>
                                            )}
                                        </MenuItem>
                                    ))
                                ) : (
                                    <MenuItem disabled>No workflow services available</MenuItem>
                                )}
                            </Select>
                        </FormControl>
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
                        Are you sure you want to delete the service type <strong>"{deletingServiceType?.name}"</strong>?
                    </Typography>
                    {deletingServiceType && deletingServiceType.countersCount > 0 && (
                        <Alert severity="warning" sx={{ mt: 2 }}>
                            This service type is currently assigned to {deletingServiceType.countersCount} counter(s). <br />
                            Please reassign or remove those counters before deleting this service type.
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
                        disabled={deleteLoading || (deletingServiceType?.countersCount ?? 0) > 0}
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
