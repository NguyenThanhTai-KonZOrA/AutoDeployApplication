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
    Chip,
    IconButton,
    Snackbar,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    Visibility as VisibilityIcon,
    FilterList as FilterListIcon,
} from "@mui/icons-material";
import { useState, useEffect } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { installationService, applicationService } from "../services/deploymentManagerService";
import type { InstallationLogRequest, InstallationLogResponse } from "../type/installationLogType";
import type { ApplicationResponse } from "../type/applicationType";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

export default function AdminInstallationLogPage() {
    useSetPageTitle(PAGE_TITLES.INSTALLATION_LOGS);
    const [logs, setLogs] = useState<InstallationLogResponse[]>([]);
    const [applications, setApplications] = useState<ApplicationResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" as "success" | "error" });

    // Pagination states
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [totalRecords, setTotalRecords] = useState(0);

    // Filter states
    const [filters, setFilters] = useState<InstallationLogRequest>({
        ApplicationId: 0,
        UserName: "",
        MachineName: "",
        Status: "",
        Action: "",
        // from start of yesterday viet nam time UTC +7
        FromDate: (() => {
            // Set to 1 day ago in local time
            const date = new Date();
            date.setDate(date.getDate() - 1);
            // Format to local datetime-local format (YYYY-MM-DDTHH:mm)
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            return `${year}-${month}-${day}T${hours}:${minutes}`;
        })(),
        // to end of today viet nam time UTC +7
        ToDate: (() => {
            // Set to current date/time in local time
            const date = new Date();
            // Format to local datetime-local format (YYYY-MM-DDTHH:mm)
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hours = String(date.getHours()).padStart(2, '0');
            const minutes = String(date.getMinutes()).padStart(2, '0');
            return `${year}-${month}-${day}T${hours}:${minutes}`;
        })(),
        Page: 1,
        PageSize: 10,
        Take: 10,
        Skip: 0,
    });

    // View detail dialog
    const [viewDialogOpen, setViewDialogOpen] = useState(false);
    const [viewingLog, setViewingLog] = useState<InstallationLogResponse | null>(null);

    const showSnackbar = (message: string, severity: "success" | "error") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadApplications = async () => {
        try {
            const data = await applicationService.getAllApplications();
            setApplications(data.filter(app => app.isActive));
        } catch (error: any) {
            console.error("Error loading applications:", error);
        }
    };

    const loadLogs = async () => {
        try {
            setLoading(true);
            setError(null);

            const request: InstallationLogRequest = {
                ...filters,
                Page: currentPage,
                PageSize: itemsPerPage,
                Take: itemsPerPage,
                Skip: (currentPage - 1) * itemsPerPage,
            };

            const response = await installationService.getInstallationLogs(request);
            setLogs(response.logs);
            setTotalRecords(response.totalRecords);
        } catch (error: any) {
            console.error("Error loading installation logs:", error);
            let errorMessage = "Failed to load installation logs";
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

    const handleFilterChange = (field: keyof InstallationLogRequest, value: string | number) => {
        setFilters(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleApplyFilters = () => {
        setCurrentPage(1);
        loadLogs();
    };

    const handleClearFilters = () => {
        setFilters({
            ApplicationId: 0,
            UserName: "",
            MachineName: "",
            Status: "",
            Action: "",
            FromDate: (() => {
                // Set to 1 day ago in local time
                const date = new Date();
                date.setDate(date.getDate() - 1);
                // Format to local datetime-local format (YYYY-MM-DDTHH:mm)
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                return `${year}-${month}-${day}T${hours}:${minutes}`;
            })(),
            // to end of today viet nam time UTC +7
            ToDate: (() => {
                // Set to current date/time in local time
                const date = new Date();
                // Format to local datetime-local format (YYYY-MM-DDTHH:mm)
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');
                return `${year}-${month}-${day}T${hours}:${minutes}`;
            })(),
            Page: 1,
            PageSize: itemsPerPage,
            Take: itemsPerPage,
            Skip: 0,
        });
        setCurrentPage(1);
    };

    const handleViewDetail = (log: InstallationLogResponse) => {
        setViewingLog(log);
        setViewDialogOpen(true);
    };

    const handleCloseViewDialog = () => {
        setViewDialogOpen(false);
        setViewingLog(null);
    };

    // Auto load data on component mount
    useEffect(() => {
        loadApplications();
        loadLogs();
    }, []);

    // Reload logs when page or items per page changes
    useEffect(() => {
        loadLogs();
    }, [currentPage, itemsPerPage]);

    const totalPages = Math.ceil(totalRecords / itemsPerPage);

    const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
        setCurrentPage(value);
    };

    const handleItemsPerPageChange = (event: any) => {
        setItemsPerPage(event.target.value);
        setCurrentPage(1);
    };

    const getStatusColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'success': return 'success';
            case 'failed': return 'error';
            case 'inprogress': return 'warning';
            default: return 'default';
        }
    };

    const getActionColor = (action: string) => {
        switch (action.toLowerCase()) {
            case 'install': return 'info';
            case 'update': return 'primary';
            case 'uninstall': return 'error';
            case 'updaterollback': return 'warning';
            case 'updateconfig': return 'default';
            default: return 'default';
        }
    };

    return (
        <AdminLayout>
            <Box>
                {/* Header */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" color="text.secondary">
                        View installation logs, track application deployments, and monitor installation status
                    </Typography>
                </Box>

                {/* Filters */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                            <FilterListIcon sx={{ mr: 1 }} /> Filters
                        </Typography>
                        <Grid container spacing={2} alignItems="center">
                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="From Date"
                                    type="datetime-local"
                                    value={filters.FromDate}
                                    onChange={(e) => handleFilterChange("FromDate", e.target.value)}
                                    InputLabelProps={{ shrink: true }}
                                    inputProps={{
                                        max: FormatUtcTime.getTodayWithTimeString(),
                                    }}
                                    onFocus={(e) => {
                                        const input = e.target as HTMLInputElement;
                                        if (input.showPicker) {
                                            input.showPicker();
                                        }
                                    }}
                                />
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="To Date"
                                    type="datetime-local"
                                    value={filters.ToDate}
                                    onChange={(e) => handleFilterChange("ToDate", e.target.value)}
                                    InputLabelProps={{ shrink: true }}
                                    inputProps={{
                                        max: FormatUtcTime.getTodayWithTimeString(),
                                        min: filters.FromDate
                                    }}
                                    onFocus={(e) => {
                                        const input = e.target as HTMLInputElement;
                                        if (input.showPicker) {
                                            input.showPicker();
                                        }
                                    }}
                                />
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <FormControl fullWidth size="small">
                                    <InputLabel>Application</InputLabel>
                                    <Select
                                        label="Application"
                                        value={filters.ApplicationId}
                                        onChange={(e) => handleFilterChange("ApplicationId", e.target.value)}
                                    >
                                        <MenuItem value={0}>All Applications</MenuItem>
                                        {applications.map((app) => (
                                            <MenuItem key={app.id} value={app.id}>
                                                {app.name}
                                            </MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="User Name"
                                    value={filters.UserName}
                                    onChange={(e) => handleFilterChange("UserName", e.target.value)}
                                />
                            </Grid>

                            {/* <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <TextField
                                    fullWidth
                                    size="small"
                                    label="Machine Name"
                                    value={filters.MachineName}
                                    onChange={(e) => handleFilterChange("MachineName", e.target.value)}
                                />
                            </Grid> */}

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <FormControl fullWidth size="small">
                                    <Select
                                        value={filters.Status}
                                        onChange={(e) => handleFilterChange("Status", e.target.value)}
                                        displayEmpty
                                    >
                                        <MenuItem value="">
                                            <em>All Status</em>
                                        </MenuItem>
                                        <MenuItem value="Success">Success</MenuItem>
                                        <MenuItem value="Failed">Failed</MenuItem>
                                        <MenuItem value="InProgress">In Progress</MenuItem>
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <FormControl fullWidth size="small">
                                    <Select
                                        value={filters.Action}
                                        onChange={(e) => handleFilterChange("Action", e.target.value)}
                                        displayEmpty
                                    >
                                        <MenuItem value="">
                                            <em>All Actions</em>
                                        </MenuItem>
                                        <MenuItem value="Install">Install</MenuItem>
                                        <MenuItem value="Update">Update</MenuItem>
                                        <MenuItem value="Uninstall">Uninstall</MenuItem>
                                        <MenuItem value="UpdateRollback">Update Rollback</MenuItem>
                                        <MenuItem value="UpdateConfig">Update Config</MenuItem>

                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <FormControl fullWidth size="small">
                                    <InputLabel>Items per page</InputLabel>
                                    <Select
                                        value={itemsPerPage}
                                        label="Items per page"
                                        onChange={handleItemsPerPageChange}
                                    >
                                        <MenuItem value={10}>10</MenuItem>
                                        <MenuItem value={20}>20</MenuItem>
                                        <MenuItem value={50}>50</MenuItem>
                                        <MenuItem value={100}>100</MenuItem>
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, md: 3 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Total: {totalRecords} installation logs
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Success: ${logs.filter(log => log.status.toLowerCase() === 'success').length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Failed: ${logs.filter(log => log.status.toLowerCase() === 'failed').length}`}
                                        color="error"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`In Progress: ${logs.filter(log => log.status.toLowerCase() === 'inprogress').length}`}
                                        color="warning"
                                        size="small"
                                        variant="outlined"
                                    />
                                </Box>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                                <Box sx={{ display: 'flex', gap: 1 }}>
                                    <Button
                                        variant="contained"
                                        startIcon={<SearchIcon />}
                                        onClick={handleApplyFilters}
                                    >
                                        Search
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        startIcon={<RefreshIcon />}
                                        onClick={loadLogs}
                                        disabled={loading}
                                    >
                                        Refresh
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        onClick={handleClearFilters}
                                    >
                                        Clear Filters
                                    </Button>
                                </Box>
                            </Grid>


                        </Grid>
                    </CardContent>
                </Card>

                {loading && <LinearProgress sx={{ mb: 2 }} />}

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }} action={
                        <IconButton size="small" onClick={loadLogs}>
                            <RefreshIcon />
                        </IconButton>
                    }>
                        {error}
                    </Alert>
                )}

                {/* Installation Logs Table */}
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
                                        {/* <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 80 }}>
                                            ID
                                        </TableCell> */}
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 150 }}>
                                            Application
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            User Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Machine Name
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Action
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Status
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }} align="center">
                                            Old Version
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }} align="center">
                                            New Version
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Duration (s)
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Started At
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Completed At
                                        </TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {loading ? (
                                        Array.from({ length: itemsPerPage }).map((_, index) => (
                                            <TableRow key={index}>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell><Skeleton width="40px" /></TableCell>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell><Skeleton width="100px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="80px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell align="center"><Skeleton width="60px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                                <TableCell><Skeleton width="150px" /></TableCell>
                                            </TableRow>
                                        ))
                                    ) : logs.length > 0 ? (
                                        logs.map((log) => (
                                            <TableRow
                                                key={log.id}
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
                                                    <IconButton
                                                        size="small"
                                                        color="info"
                                                        onClick={() => handleViewDetail(log)}
                                                    >
                                                        <VisibilityIcon />
                                                    </IconButton>
                                                </TableCell>
                                                {/* <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {log.id}
                                                </TableCell> */}
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {applications.find(app => app.id === log.applicationId)?.name || `App #${log.applicationId}`}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.userName}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.machineName}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={log.action}
                                                        color={getActionColor(log.action)}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={log.status}
                                                        color={getStatusColor(log.status)}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.oldVersion || '-'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.newVersion || '-'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.durationInSeconds > 0 ? log.durationInSeconds : '-'}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {FormatUtcTime.formatDateTime(log.startedAt)}
                                                </TableCell>
                                                <TableCell sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {log.completedAt ? FormatUtcTime.formatDateTime(log.completedAt) : '-'}
                                                </TableCell>
                                            </TableRow>
                                        ))
                                    ) : (
                                        <TableRow>
                                            <TableCell colSpan={12} sx={{ textAlign: 'center', py: 4 }}>
                                                <Typography color="text.secondary">
                                                    No installation logs found
                                                </Typography>
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </TableContainer>

                        {/* Pagination */}
                        {totalRecords > 0 && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', mt: 2, mb: 2, gap: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Showing {logs.length > 0 ? ((currentPage - 1) * itemsPerPage) + 1 : 0}-{Math.min(currentPage * itemsPerPage, totalRecords)} of {totalRecords} items
                                </Typography>
                                {totalRecords > itemsPerPage && (
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

            {/* View Detail Dialog */}
            <Dialog
                open={viewDialogOpen}
                onClose={handleCloseViewDialog}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle>Installation Log Details</DialogTitle>
                <DialogContent>
                    {viewingLog && (
                        <Box sx={{ mt: 2 }}>
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Log ID</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.id}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Application</Typography>
                                    <Typography variant="body1" fontWeight={500}>
                                        {applications.find(app => app.id === viewingLog.applicationId)?.name || `App #${viewingLog.applicationId}`}
                                    </Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">User Name</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.userName}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Machine Name</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.machineName}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Action</Typography>
                                    <Chip label={viewingLog.action} color={getActionColor(viewingLog.action)} size="small" />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Status</Typography>
                                    <Chip label={viewingLog.status} color={getStatusColor(viewingLog.status)} size="small" />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Old Version</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.oldVersion || '-'}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">New Version</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.newVersion || '-'}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Typography variant="body2" color="text.secondary">Installation Path</Typography>
                                    <Typography variant="body1" fontWeight={500}>{viewingLog.installationPath || '-'}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Started At</Typography>
                                    <Typography variant="body1" fontWeight={500}>{FormatUtcTime.formatDateTime(viewingLog.startedAt)}</Typography>
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <Typography variant="body2" color="text.secondary">Completed At</Typography>
                                    <Typography variant="body1" fontWeight={500}>
                                        {viewingLog.completedAt ? FormatUtcTime.formatDateTime(viewingLog.completedAt) : '-'}
                                    </Typography>
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Typography variant="body2" color="text.secondary">Duration</Typography>
                                    <Typography variant="body1" fontWeight={500}>
                                        {viewingLog.durationInSeconds > 0 ? `${viewingLog.durationInSeconds} seconds` : '-'}
                                    </Typography>
                                </Grid>
                                {viewingLog.errorMessage && (
                                    <Grid size={{ xs: 12 }}>
                                        <Typography variant="body2" color="text.secondary">Error Message</Typography>
                                        <Alert severity="error" sx={{ mt: 1 }}>
                                            {viewingLog.errorMessage}
                                        </Alert>
                                    </Grid>
                                )}
                                {viewingLog.stackTrace && (
                                    <Grid size={{ xs: 12 }}>
                                        <Typography variant="body2" color="text.secondary">Stack Trace</Typography>
                                        <Paper variant="outlined" sx={{ p: 2, mt: 1, bgcolor: '#f5f5f5', maxHeight: 300, overflow: 'auto' }}>
                                            <Typography variant="body2" component="pre" sx={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                                                {viewingLog.stackTrace}
                                            </Typography>
                                        </Paper>
                                    </Grid>
                                )}
                            </Grid>
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseViewDialog}>Close</Button>
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
