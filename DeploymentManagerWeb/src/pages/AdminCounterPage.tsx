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
    List,
    ListItem,
    ListItemText,
    ListItemSecondaryAction,
    Checkbox,
    FormControlLabel,
    Divider
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    ToggleOn as ToggleOnIcon,
    ToggleOff as ToggleOffIcon,
    Computer as ComputerIcon,
    ContentCopy as ContentCopyIcon,
    Edit as EditIcon
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { counterService, queueAdminService, serviceTypeService } from "../services/queueService";
import type {
    CountersReportResponse,
    CurrentCounterHostNameResponse,
    SummaryServiceTypeResponse,
    CounterWithServiceTypesResponse,
    UpdateCounterServiceTypesRequest,
    CounterServiceTypeItem
} from "../type/type";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

export default function AdminCounterPage() {
    useSetPageTitle(PAGE_TITLES.COUNTERS);
    const [counters, setCounters] = useState<CountersReportResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [toggleLoading, setToggleLoading] = useState<number | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [editingValue, setEditingValue] = useState<string>('');
    const [saving, setSaving] = useState(false);
    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" as "success" | "error" });

    // Hostname check states
    const [hostnameDialogOpen, setHostnameDialogOpen] = useState(false);
    const [hostnameData, setHostnameData] = useState<CurrentCounterHostNameResponse | null>(null);
    const [hostnameLoading, setHostnameLoading] = useState(false);

    // Service Types dialog states
    const [serviceTypesDialogOpen, setServiceTypesDialogOpen] = useState(false);
    const [serviceTypesLoading, setServiceTypesLoading] = useState(false);
    const [allServiceTypes, setAllServiceTypes] = useState<SummaryServiceTypeResponse[]>([]);
    const [selectedServiceTypes, setSelectedServiceTypes] = useState<number[]>([]);
    const [currentEditingCounter, setCurrentEditingCounter] = useState<CountersReportResponse | null>(null);
    const [serviceTypesSaving, setServiceTypesSaving] = useState(false);

    const showSnackbar = (message: string, severity: "success" | "error") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCheckHostname = async () => {
        try {
            setHostnameLoading(true);
            const data = await queueAdminService.getCurrentHostNameCounter();
            setHostnameData(data);
            setHostnameDialogOpen(true);
        } catch (error: any) {
            console.error("Error getting hostname:", error);

            let errorMessage = "Error getting hostname information";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }

            showSnackbar(errorMessage, "error");
        } finally {
            setHostnameLoading(false);
        }
    };

    const handleCopyToClipboard = async (text: string, label: string) => {
        try {
            // Try modern clipboard API first
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(text);
                showSnackbar(`${label} copied to clipboard!`, "success");
            } else {
                // Fallback for non-secure contexts or older browsers
                const textArea = document.createElement("textarea");
                textArea.value = text;
                textArea.style.position = "fixed";
                textArea.style.left = "-999999px";
                textArea.style.top = "-999999px";
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();

                try {
                    const successful = document.execCommand('copy');
                    if (successful) {
                        showSnackbar(`${label} copied to clipboard!`, "success");
                    } else {
                        throw new Error("Copy command was unsuccessful");
                    }
                } finally {
                    document.body.removeChild(textArea);
                }
            }
        } catch (error) {
            console.error("Failed to copy to clipboard:", error);
            showSnackbar(`Failed to copy ${label}. Please copy manually: ${text}`, "error");
        }
    };

    const handleDoubleClick = (counter: any) => {
        setEditingId(counter.id);
        setEditingValue(counter.hostName);
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setEditingValue(e.target.value);
    };

    const handleSave = async (counter: any) => {
        try {
            // Check if hostname already exists (only if different from current and not empty)
            var checkedHostName = await counterService.checkedHostName({ hostName: editingValue });
            if (editingValue.trim() !== "" && editingValue !== counter.hostName) {
                if (checkedHostName) {
                    showSnackbar('Host name already exists. Please choose a different one.', "error");
                    return;
                }
            }

            setSaving(checkedHostName);
            const result = await counterService.changeHostNameCounter({
                counterId: counter.id,
                hostName: editingValue
            });

            if (result.isChangeSuccess) {
                showSnackbar(`Change Host name for Counter ${counter.id} Success!`, "success");
                counter.hostName = editingValue;
                setEditingId(null);
            }
        } catch (error: any) {
            console.error("Error updating counter host name:", error);

            // Handle HTTP error responses (400, 500, etc.)
            let errorMessage = "Error updating counter host name";

            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.response?.data?.data) {
                errorMessage = typeof error.response.data.data === 'string'
                    ? error.response.data.data
                    : (error.response.data.data?.message || JSON.stringify(error.response.data.data));
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
            throw error;
        } finally {
            setSaving(false);
        }
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadCounters = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await counterService.getCountersReport();
            setCounters(data);
        } catch (error) {
            console.error("Error loading counters:", error);
            setError("Failed to load counters data");
        } finally {
            setLoading(false);
        }
    };

    const handleToggleStatus = async (counterId: number) => {
        try {
            setToggleLoading(counterId);
            const result = await counterService.changeStatusCounter(counterId);

            if (result.isChangeSuccess) {
                showSnackbar(`Change Status for Counter ${counterId} Success!`, "success");
                setCounters(prevCounters =>
                    prevCounters.map(counter =>
                        counter.id === counterId
                            ? {
                                ...counter,
                                status: !counter.status,
                                statusName: !counter.status ? "Active" : "Inactive"
                            }
                            : counter
                    )
                );
            } else {
                setError("Failed to change counter status");
            }
        } catch (error: any) {
            console.error("Error updating counter status:", error);

            // Handle HTTP error responses (400, 500, etc.)
            let errorMessage = "Error updating counter status";

            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.response?.data?.data) {
                errorMessage = typeof error.response.data.data === 'string'
                    ? error.response.data.data
                    : (error.response.data.data?.message || JSON.stringify(error.response.data.data));
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
            throw error;
        } finally {
            setToggleLoading(null);
        }
    };

    // Service Types Dialog Handlers
    const handleOpenServiceTypesDialog = async (counter: CountersReportResponse) => {
        try {
            if (counter.isBusy) {
                showSnackbar("Cannot edit service types while counter is busy.", "error");
                return;
            }

            setCurrentEditingCounter(counter);
            setServiceTypesDialogOpen(true);
            setServiceTypesLoading(true);

            // Load all available service types
            const allTypes = await serviceTypeService.getSummarryServiceTypes();
            setAllServiceTypes(allTypes);

            // Extract IDs of already assigned service types from counter data
            if (counter.serviceTypes && counter.serviceTypes.length > 0) {
                const assignedIds = counter.serviceTypes.map(st => st.id);
                console.log("Assigned service type IDs:", assignedIds); // Debug log
                setSelectedServiceTypes(assignedIds);
            } else {
                setSelectedServiceTypes([]);
            }
        } catch (error: any) {
            console.error("Error loading service types:", error);
            let errorMessage = "Error loading service types";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setServiceTypesLoading(false);
        }
    };

    const handleCloseServiceTypesDialog = () => {
        setServiceTypesDialogOpen(false);
        setCurrentEditingCounter(null);
        setSelectedServiceTypes([]);
        setAllServiceTypes([]);
    };

    const handleToggleServiceType = (serviceTypeId: number) => {
        setSelectedServiceTypes(prev => {
            if (prev.includes(serviceTypeId)) {
                const newSelection = prev.filter(id => id !== serviceTypeId);
                console.log("Removed service type:", serviceTypeId, "New selection:", newSelection);
                return newSelection;
            } else {
                const newSelection = [...prev, serviceTypeId];
                console.log("Added service type:", serviceTypeId, "New selection:", newSelection);
                return newSelection;
            }
        });
    };

    const handleSelectAllServiceTypes = () => {
        if (selectedServiceTypes.length === allServiceTypes.length) {
            setSelectedServiceTypes([]);
        } else {
            setSelectedServiceTypes(allServiceTypes.map(st => st.id));
        }
    };

    const handleSaveServiceTypes = async () => {
        if (!currentEditingCounter) return;

        try {
            setServiceTypesSaving(true);

            const request: UpdateCounterServiceTypesRequest = {
                counterId: currentEditingCounter.id,
                serviceTypes: selectedServiceTypes.map((id, index) => ({
                    serviceTypeId: id,
                    priority: index + 1
                }))
            };

            const result = await counterService.updateCounterServiceTypes(request);

            if (result) {
                showSnackbar(`Service types updated successfully for Counter ${currentEditingCounter.id}!`, "success");
                handleCloseServiceTypesDialog();
                // Optionally reload counters to reflect changes
                loadCounters();
            }
        } catch (error: any) {
            console.error("Error updating service types:", error);
            let errorMessage = "Error updating service types";
            if (error?.response?.data?.message) {
                errorMessage = error.response.data.message;
            } else if (error?.message) {
                errorMessage = error.message;
            }
            showSnackbar(errorMessage, "error");
        } finally {
            setServiceTypesSaving(false);
        }
    };

    // Auto load data on component mount
    useEffect(() => {
        loadCounters();
    }, []);

    // Search and pagination logic
    const filteredCounters = useMemo(() => {
        if (!searchTerm) return counters;

        return counters.filter(counter =>
            counter.counterName.toLowerCase().includes(searchTerm.toLowerCase()) ||
            counter.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
            counter.hostName.toLowerCase().includes(searchTerm.toLowerCase()) ||
            counter.statusName.toLowerCase().includes(searchTerm.toLowerCase())
        );
    }, [counters, searchTerm]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredCounters.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredCounters.slice(startIndex, endIndex);

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

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    return (
        <AdminLayout>
            <Box>
                {/* Header */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="h4" fontWeight={600} sx={{ mb: 1 }}>
                    </Typography>
                    <Typography variant="body1" color="text.secondary">
                        Manage counter status and view performance statistics
                    </Typography>
                </Box>

                {/* Search and Actions */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Grid container spacing={3} alignItems="center">
                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="Search counters..."
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
                                    onClick={loadCounters}
                                    disabled={loading}
                                >
                                    Refresh
                                </Button>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <Button
                                    variant="outlined"
                                    fullWidth
                                    startIcon={<ComputerIcon />}
                                    onClick={handleCheckHostname}
                                    disabled={hostnameLoading}
                                >
                                    Check Hostname
                                </Button>
                            </Grid>

                            <Grid size={{ xs: 6, sm: 6, md: 2 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Total: {filteredCounters.length} counters
                                    {searchTerm && ` (filtered from ${counters.length})`}
                                </Typography>
                            </Grid>

                            <Grid size={{ xs: 6, sm: 6, md: 2 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Active:
                                    <Chip label={counters.filter(c => c.status).length} color="success" size="small" variant="outlined" />
                                    &nbsp;
                                    Inactive:
                                    <Chip label={counters.filter(c => !c.status).length} color="error" size="small" variant="outlined" />
                                </Typography>
                            </Grid>

                        </Grid>
                    </CardContent>
                </Card>

                {loading && <LinearProgress sx={{ mb: 2 }} />}

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }} action={
                        <IconButton size="small" onClick={loadCounters}>
                            <RefreshIcon />
                        </IconButton>
                    }>
                        {error}
                    </Alert>
                )}

                {/* Counters Table */}
                <Card>
                    <CardContent sx={{ p: 0 }}>
                        <TableContainer component={Paper} variant="outlined">
                            <Table>
                                <TableHead>
                                    <TableRow sx={{ bgcolor: 'grey.50' }}>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 20 }}>
                                            No.
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 150 }}>
                                            Counter Name
                                        </TableCell>
                                        {/* <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Description
                                        </TableCell> */}
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Host Name
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Ticket Serving
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Is Busy
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Avg Waiting Time
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Avg Serving Time
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Status
                                        </TableCell>
                                        {/* <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 140 }}>
                                            Created At
                                        </TableCell> */}

                                        {/* <TableCell sx={{ fontWeight: 600, minWidth: 120 }}>
                                            Created By
                                        </TableCell> */}
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Service Type
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Active
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 700, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Updated At
                                        </TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {loading ? (
                                        // Loading skeleton rows
                                        Array.from({ length: itemsPerPage }).map((_, index) => (
                                            <TableRow key={index}>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell><Skeleton width="120px" /></TableCell>
                                                <TableCell><Skeleton width="80px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                            </TableRow>
                                        ))
                                    ) : currentPageData.length > 0 ? (
                                        currentPageData.map((counter) => (
                                            <TableRow
                                                key={counter.id}
                                                sx={{
                                                    '&:nth-of-type(even)': { bgcolor: '#f8f9fa' },
                                                    '&:hover': { bgcolor: '#e3f2fd' }
                                                }}
                                            >
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {counter.id}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {counter.counterName}
                                                </TableCell>
                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.description}
                                                </TableCell> */}
                                                <TableCell
                                                    sx={{ borderRight: '1px solid #e0e0e0', cursor: 'pointer' }}
                                                    onDoubleClick={() => handleDoubleClick(counter)}
                                                >
                                                    {editingId === counter.id ? (
                                                        <TextField
                                                            value={editingValue}
                                                            onChange={handleChange}
                                                            size="small"
                                                            variant="outlined"
                                                            autoFocus
                                                            onBlur={() => handleSave(counter)}
                                                            onKeyDown={(e) => {
                                                                if (e.key === 'Enter') handleSave(counter);
                                                                if (e.key === 'Escape') setEditingId(null);
                                                            }}
                                                            disabled={saving}
                                                            sx={{ width: '100%' }}
                                                        />
                                                    ) : (
                                                        <Typography>{counter.hostName}</Typography>
                                                    )}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.ticketNumberServing === 0 ? '-' : counter.ticketNumberServing}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.isBusy ? (
                                                        <Chip label="Busy" color="error" size="small" variant="outlined" />
                                                    ) : (
                                                        <Chip label="Available" color="success" size="small" variant="outlined" />
                                                    )}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.averageWaitingTime}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.averageServingTime}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={counter.statusName}
                                                        color={counter.status ? "success" : "error"}
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(counter.createdAt)}
                                                </TableCell> */}

                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {counter.createdBy}
                                                </TableCell> */}
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap', justifyContent: 'center' }}>
                                                        {counter.serviceTypes && counter.serviceTypes.length > 0 ? (
                                                            counter.serviceTypes.map((st) => (
                                                                <Chip key={st.id} label={st.name} size="small" color="primary" variant="outlined" />
                                                            ))
                                                        ) : (
                                                            <Typography variant="caption" color="text.secondary">
                                                                No service types
                                                            </Typography>
                                                        )}
                                                        <Tooltip title="Edit service types">
                                                            <IconButton
                                                                size="small"
                                                                color="primary"
                                                                onClick={() => handleOpenServiceTypesDialog(counter)}
                                                            >
                                                                <EditIcon fontSize="small" />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </Box>
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Tooltip title={`${counter.status ? 'Deactivate' : 'Activate'} counter`}>
                                                        <span>
                                                            <Switch
                                                                checked={counter.status}
                                                                onChange={() => handleToggleStatus(counter.id)}
                                                                disabled={toggleLoading === counter.id}
                                                                color="success"
                                                                size="small"
                                                            />
                                                        </span>
                                                    </Tooltip>
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(counter.updatedAt)} <br />
                                                    <Typography variant="caption" color="text.secondary">
                                                        by {counter.updatedBy}
                                                    </Typography>
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={8} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    {searchTerm ? `No counters found matching "${searchTerm}"` : "No counters available"}
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {filteredCounters.length > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {filteredCounters.length > 0 ? startIndex + 1 : 0}-{Math.min(endIndex, filteredCounters.length)} of {filteredCounters.length} items
                                </Typography>
                                {filteredCounters.length > itemsPerPage && (
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

            {/* Hostname Information Dialog */}
            <Dialog
                open={hostnameDialogOpen}
                onClose={() => setHostnameDialogOpen(false)}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <ComputerIcon />
                    Current Computer Information
                </DialogTitle>
                <DialogContent>
                    {hostnameData && (
                        <List>
                            <ListItem sx={{ px: 0 }}>
                                <ListItemText
                                    primary="Computer Name"
                                    secondary={hostnameData.computerName}
                                    primaryTypographyProps={{ fontWeight: 600 }}
                                    secondaryTypographyProps={{
                                        fontSize: '1.1rem',
                                        color: 'primary.main',
                                        fontFamily: 'monospace'
                                    }}
                                />
                                <ListItemSecondaryAction>
                                    {/* <Tooltip title="Copy Computer Name">
                                        <IconButton
                                            edge="end"
                                            onClick={() => handleCopyToClipboard(hostnameData.computerName, "Computer Name")}
                                            size="small"
                                        >
                                            <ContentCopyIcon />
                                        </IconButton>
                                    </Tooltip> */}
                                </ListItemSecondaryAction>
                            </ListItem>

                            <ListItem sx={{ px: 0 }}>
                                <ListItemText
                                    primary="IP Address"
                                    secondary={hostnameData.ip}
                                    primaryTypographyProps={{ fontWeight: 600 }}
                                    secondaryTypographyProps={{
                                        fontSize: '1.1rem',
                                        color: 'secondary.main',
                                        fontFamily: 'monospace'
                                    }}
                                />
                                <ListItemSecondaryAction>
                                    {/* <Tooltip title="Copy IP Address">
                                        <IconButton
                                            edge="end"
                                            onClick={() => handleCopyToClipboard(hostnameData.ip, "IP Address")}
                                            size="small"
                                        >
                                            <ContentCopyIcon />
                                        </IconButton>
                                    </Tooltip> */}
                                </ListItemSecondaryAction>
                            </ListItem>
                        </List>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setHostnameDialogOpen(false)} variant="contained">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Service Types Dialog */}
            <Dialog
                open={serviceTypesDialogOpen}
                onClose={handleCloseServiceTypesDialog}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    Manage Service Types - Counter {currentEditingCounter?.id}
                    {/* Debug info */}
                    {currentEditingCounter && (
                        <Typography variant="caption" display="block" color="text.secondary">
                            Current counter has {currentEditingCounter.serviceTypes?.length || 0} service type(s)
                        </Typography>
                    )}
                </DialogTitle>
                <DialogContent>
                    {serviceTypesLoading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                            <LinearProgress sx={{ width: '100%' }} />
                        </Box>
                    ) : (
                        <Box sx={{ mt: 2 }}>
                            {/* Debug info */}
                            <Alert severity="success" sx={{ mb: 2 }}>
                                <Typography variant="body2">
                                    All service types: {allServiceTypes.length} |
                                    Selected: {selectedServiceTypes.length}
                                    {/* IDs: [{selectedServiceTypes.join(', ')}] */}
                                </Typography>
                            </Alert>

                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={selectedServiceTypes.length === allServiceTypes.length && allServiceTypes.length > 0}
                                        indeterminate={selectedServiceTypes.length > 0 && selectedServiceTypes.length < allServiceTypes.length}
                                        onChange={handleSelectAllServiceTypes}
                                    />
                                }
                                label={<Typography fontWeight={600}>Select All</Typography>}
                            />
                            <Divider sx={{ my: 2 }} />

                            {allServiceTypes.length > 0 ? (
                                <Box>
                                    <List>
                                        {allServiceTypes.map((serviceType) => (
                                            <ListItem key={serviceType.id} disablePadding>
                                                <FormControlLabel
                                                    control={
                                                        <Checkbox
                                                            checked={selectedServiceTypes.includes(serviceType.id)}
                                                            onChange={() => handleToggleServiceType(serviceType.id)}
                                                        />
                                                    }
                                                    label={
                                                        <Box>
                                                            <Typography variant="body1" fontWeight={500}>
                                                                {serviceType.name}
                                                            </Typography>
                                                            {serviceType.description && (
                                                                <Typography variant="caption" color="text.secondary">
                                                                    {serviceType.description}
                                                                </Typography>
                                                            )}
                                                        </Box>
                                                    }
                                                    sx={{ width: '100%', py: 1 }}
                                                />
                                            </ListItem>
                                        ))}
                                    </List>

                                    {selectedServiceTypes.length > 0 && (
                                        <Box sx={{ mt: 2, p: 2, bgcolor: '#f0f0f0', borderRadius: 1 }}>
                                            <Typography variant="body2" color="primary.dark" fontWeight={600}>
                                                Selected: {selectedServiceTypes.length} service type(s)
                                            </Typography>
                                        </Box>
                                    )}
                                </Box>
                            ) : (
                                <Alert severity="info">
                                    No service types available. Please create service types first.
                                </Alert>
                            )}
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button
                        onClick={handleCloseServiceTypesDialog}
                        disabled={serviceTypesSaving}
                    >
                        Cancel
                    </Button>
                    <Button
                        onClick={handleSaveServiceTypes}
                        variant="contained"
                        disabled={serviceTypesSaving || serviceTypesLoading}
                    >
                        {serviceTypesSaving ? "Saving..." : "Save"}
                    </Button>
                </DialogActions>
            </Dialog>

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
