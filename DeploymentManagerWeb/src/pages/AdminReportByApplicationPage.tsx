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
    Chip,
    IconButton,
    Tooltip,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Stack,
    Accordion,
    AccordionSummary,
    AccordionDetails,
    type SelectChangeEvent,
} from "@mui/material";
import {
    Search as SearchIcon,
    Refresh as RefreshIcon,
    Visibility as VisibilityIcon,
    ExpandMore as ExpandMoreIcon,
    Computer as ComputerIcon,
    PersonOutline as PersonIcon,
    FilterList as FilterListIcon
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { installationService, applicationService } from "../services/deploymentManagerService";
import type { InstallationReportRequest, InstallationReportResponse, VersionInstallationStats, PCInstallationDetail } from "../type/installationLogType";
import type { ApplicationResponse } from "../type/applicationType";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";

export default function AdminReportByApplicationPage() {
    useSetPageTitle(PAGE_TITLES.REPORT_BY_APPLICATION || "Report by Application");

    // Data states
    const [reports, setReports] = useState<InstallationReportResponse[]>([]);
    const [applications, setApplications] = useState<ApplicationResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Search and filter states
    const [searchTerm, setSearchTerm] = useState("");
    const [filterApplicationId, setFilterApplicationId] = useState<number | "">("");
    const [filterMachineName, setFilterMachineName] = useState("");
    const [filterStatus, setFilterStatus] = useState<string>("");
    const [filterFromDate, setFilterFromDate] = useState<string>(() => {
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
    });
    const [filterToDate, setFilterToDate] = useState<string>(() => {
        // Set to current date/time in local time
        const date = new Date();
        // Format to local datetime-local format (YYYY-MM-DDTHH:mm)
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day}T${hours}:${minutes}`;
    });

    // Pagination states
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);

    // Dialog states
    const [detailDialogOpen, setDetailDialogOpen] = useState(false);
    const [selectedReport, setSelectedReport] = useState<InstallationReportResponse | null>(null);

    // Load applications for filter dropdown
    useEffect(() => {
        const fetchApplications = async () => {
            try {
                const apps = await applicationService.getAllApplications();
                setApplications(apps);
            } catch (err) {
                console.error("Failed to fetch applications:", err);
            }
        };
        fetchApplications();
    }, []);

    // Load reports
    const fetchReports = async () => {
        setLoading(true);
        setError(null);
        try {
            const request: InstallationReportRequest = {
                ApplicationId: filterApplicationId || undefined,
                MachineName: filterMachineName || undefined,
                Status: filterStatus || undefined,
                FromDate: filterFromDate || undefined,
                ToDate: filterToDate || undefined,
            };
            const data = await installationService.getReportByApplication(request);
            setReports(data);
        } catch (err) {
            setError("Failed to load reports. Please try again.");
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchReports();
    }, [filterApplicationId, filterMachineName, filterStatus, filterFromDate, filterToDate]);

    // Filter and search
    const filteredReports = useMemo(() => {
        if (!searchTerm) return reports;

        const lowerSearch = searchTerm.toLowerCase();
        return reports.filter(
            (report) =>
                report.applicationName.toLowerCase().includes(lowerSearch) ||
                report.appCode.toLowerCase().includes(lowerSearch)
        );
    }, [reports, searchTerm]);

    // Pagination
    const totalPages = Math.ceil(filteredReports.length / itemsPerPage);
    const paginatedReports = useMemo(() => {
        const startIndex = (currentPage - 1) * itemsPerPage;
        return filteredReports.slice(startIndex, startIndex + itemsPerPage);
    }, [filteredReports, currentPage, itemsPerPage]);

    const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
        setCurrentPage(value);
    };

    const handleSearch = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(event.target.value);
        setCurrentPage(1); // Reset to first page when searching
    };

    const handleRefresh = () => {
        fetchReports();
    };

    const handleViewDetails = (report: InstallationReportResponse) => {
        setSelectedReport(report);
        setDetailDialogOpen(true);
    };

    const handleCloseDialog = () => {
        setDetailDialogOpen(false);
        setSelectedReport(null);
    };

    const getStatusColor = (status: string) => {
        switch (status?.toLowerCase()) {
            case "success":
                return "success";
            case "failed":
                return "error";
            case "inprogress":
                return "warning";
            default:
                return "default";
        }
    };

    const handleClearFilters = () => {
        setFilterApplicationId("");
        setFilterMachineName("");
        setFilterStatus("");
        setFilterFromDate("");
        setFilterToDate("");
        setSearchTerm("");
        setCurrentPage(1);
    };

    return (
        <AdminLayout>
            <Box>
                {/* Header */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" color="text.secondary">
                        Reports by application, filter and search reports, and view detailed information
                    </Typography>
                </Box>

                {/* Filters */}
                <Card sx={{ mb: 3 }}>
                    <CardContent>
                        <Typography variant="h6" sx={{ mb: 2, fontWeight: 600, display: 'flex', alignItems: 'center' }}>
                            <FilterListIcon sx={{ mr: 1 }} /> Filters
                        </Typography>
                        <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" } }}>
                            <TextField
                                fullWidth
                                size="small"
                                label="From Date"
                                type="datetime-local"
                                value={filterFromDate}
                                onChange={(e) => {
                                    setFilterFromDate(e.target.value);
                                    setCurrentPage(1);
                                }}
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

                            <TextField
                                fullWidth
                                size="small"
                                label="To Date"
                                type="datetime-local"
                                value={filterToDate}
                                onChange={(e) => {
                                    setFilterToDate(e.target.value);
                                    setCurrentPage(1);
                                }}
                                InputLabelProps={{ shrink: true }}
                                inputProps={{
                                    max: FormatUtcTime.getTodayWithTimeString(),
                                    min: filterFromDate
                                }}
                                onFocus={(e) => {
                                    const input = e.target as HTMLInputElement;
                                    if (input.showPicker) {
                                        input.showPicker();
                                    }
                                }}
                            />
                            <FormControl fullWidth size="small">
                                <InputLabel>Application</InputLabel>
                                <Select
                                    value={filterApplicationId}
                                    label="Application"
                                    onChange={(e: SelectChangeEvent<number | "">) => {
                                        setFilterApplicationId(e.target.value as number | "");
                                        setCurrentPage(1);
                                    }}
                                >
                                    <MenuItem value={0} selected>All Applications</MenuItem>
                                    {applications.map((app) => (
                                        <MenuItem key={app.id} value={app.id}>
                                            {app.name} ({app.appCode})
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>

                            <TextField
                                size="small"
                                placeholder="Search by application name or code..."
                                value={searchTerm}
                                onChange={handleSearch}
                                InputProps={{
                                    startAdornment: <SearchIcon sx={{ mr: 1, color: "text.secondary" }} />,
                                }}
                                sx={{ flexGrow: 1, minWidth: 250 }}
                            />
                            <TextField
                                fullWidth
                                size="small"
                                label="Machine Name"
                                value={filterMachineName}
                                onChange={(e) => {
                                    setFilterMachineName(e.target.value);
                                    setCurrentPage(1);
                                }}
                            />

                            <FormControl fullWidth size="small">
                                <InputLabel>Status</InputLabel>
                                <Select
                                    value={filterStatus}
                                    label="Status"
                                    onChange={(e) => {
                                        setFilterStatus(e.target.value);
                                        setCurrentPage(1);
                                    }}
                                >
                                    <MenuItem value="">All Status</MenuItem>
                                    <MenuItem value="Success">Success</MenuItem>
                                    <MenuItem value="Failed">Failed</MenuItem>
                                    <MenuItem value="InProgress">In Progress</MenuItem>
                                </Select>
                            </FormControl>

                            <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
                                <Button
                                    variant="contained"
                                    startIcon={<RefreshIcon />}
                                    onClick={handleRefresh}
                                    disabled={loading}
                                >
                                    Refresh
                                </Button>

                                <Button
                                    variant="outlined"
                                    onClick={handleClearFilters}
                                    sx={{ minWidth: { xs: "100%", md: "auto" } }}
                                >
                                    Clear Filters
                                </Button>

                                <FormControl size="small" sx={{ minWidth: 120 }}>
                                    <InputLabel>Items per page</InputLabel>
                                    <Select
                                        value={itemsPerPage}
                                        label="Items per page"
                                        onChange={(e) => {
                                            setItemsPerPage(Number(e.target.value));
                                            setCurrentPage(1);
                                        }}
                                    >
                                        <MenuItem value={5}>5</MenuItem>
                                        <MenuItem value={10}>10</MenuItem>
                                        <MenuItem value={25}>25</MenuItem>
                                        <MenuItem value={50}>50</MenuItem>
                                    </Select>
                                </FormControl>
                            </Box>
                        </Box>
                    </CardContent>
                </Card>

                {/* Table */}
                <Card>
                    <CardContent>
                        {loading && <LinearProgress sx={{ mb: 2 }} />}
                        {error && (
                            <Alert severity="error" sx={{ mb: 2 }}>
                                {error}
                            </Alert>
                        )}

                        {!loading && filteredReports.length === 0 ? (
                            <Box sx={{ textAlign: "center", py: 4 }}>
                                <Typography variant="body1" color="text.secondary">
                                    No reports found
                                </Typography>
                            </Box>
                        ) : (
                            <>
                                <TableContainer component={Paper} variant="outlined">
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell sx={{ fontWeight: 600 }}>Application</TableCell>
                                                <TableCell sx={{ fontWeight: 600 }}>App Code</TableCell>
                                                <TableCell sx={{ fontWeight: 600 }} align="center">
                                                    Total Versions
                                                </TableCell>
                                                <TableCell sx={{ fontWeight: 600 }} align="center">
                                                    Total PCs
                                                </TableCell>
                                                <TableCell sx={{ fontWeight: 600 }} align="center">
                                                    Actions
                                                </TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {paginatedReports.map((report) => (
                                                <TableRow key={report.applicationId} hover>
                                                    <TableCell>{report.applicationName}</TableCell>
                                                    <TableCell>
                                                        <Chip label={report.appCode} size="small" />
                                                    </TableCell>
                                                    <TableCell align="center">
                                                        <Chip
                                                            label={report.versionStats.length}
                                                            color="primary"
                                                            size="small"
                                                            variant="outlined"
                                                        />
                                                    </TableCell>
                                                    <TableCell align="center">
                                                        <Chip
                                                            label={report.totalPCs}
                                                            color="secondary"
                                                            size="small"
                                                            variant="outlined"
                                                        />
                                                    </TableCell>
                                                    <TableCell align="center">
                                                        <Tooltip title="View Details">
                                                            <IconButton
                                                                size="small"
                                                                onClick={() => handleViewDetails(report)}
                                                                color="primary"
                                                            >
                                                                <VisibilityIcon />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </TableContainer>

                                {/* Pagination */}
                                {totalPages > 1 && (
                                    <Box sx={{ display: "flex", justifyContent: "center", mt: 3 }}>
                                        <Pagination
                                            count={totalPages}
                                            page={currentPage}
                                            onChange={handlePageChange}
                                            color="primary"
                                            showFirstButton
                                            showLastButton
                                        />
                                    </Box>
                                )}

                                <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                                    Showing {paginatedReports.length} of {filteredReports.length} report(s)
                                    {searchTerm && ` (filtered from ${reports.length} total)`}
                                </Typography>
                            </>
                        )}
                    </CardContent>
                </Card>

                {/* Detail Dialog */}
                <Dialog
                    open={detailDialogOpen}
                    onClose={handleCloseDialog}
                    maxWidth="lg"
                    fullWidth
                >
                    <DialogTitle>
                        <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <Typography variant="h6" sx={{ fontWeight: 600 }}>
                                {selectedReport?.applicationName} - Installation Report
                            </Typography>
                            <Chip label={selectedReport?.appCode} color="primary" size="small" />
                        </Box>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                            Total PCs: {selectedReport?.totalPCs}
                        </Typography>
                    </DialogTitle>
                    <DialogContent dividers>
                        {selectedReport?.versionStats && selectedReport.versionStats.length > 0 ? (
                            <Stack spacing={2}>
                                <Typography variant="h6" sx={{ fontWeight: 600, mb: 1 }}>
                                    Version Installation Statistics
                                </Typography>
                                {selectedReport.versionStats.map((versionStat: VersionInstallationStats, index: number) => (
                                    <Accordion key={index} defaultExpanded={index === 0}>
                                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                                            <Box sx={{ display: "flex", alignItems: "center", gap: 2, width: "100%" }}>
                                                <Typography sx={{ fontWeight: 600 }}>
                                                    Version: {versionStat.version}
                                                </Typography>
                                                <Chip
                                                    label={`Machines installed: ${versionStat.pcCount} PC${versionStat.pcCount > 1 ? 's' : ''}`}
                                                    size="small"
                                                    color="primary"
                                                    variant="outlined"
                                                />
                                                <Chip
                                                    label={`Machines updated: ${versionStat.updatedPCCount} PC${versionStat.updatedPCCount > 1 ? 's' : ''}`}
                                                    size="small"
                                                    color="success"
                                                    variant="outlined"
                                                />
                                                {selectedReport.versionStats.length !== 1 && index < 1 &&

                                                    <Chip
                                                        label={`Machines out of date: ${versionStat.notUpdatedPCCount} PC${versionStat.notUpdatedPCCount > 1 ? 's' : ''}`}
                                                        size="small"
                                                        color="warning"
                                                        variant="outlined"
                                                    />
                                                }
                                            </Box>
                                        </AccordionSummary>
                                        <AccordionDetails>
                                            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 2 }}>
                                                PC Installation Details:
                                            </Typography>
                                            <TableContainer component={Paper} variant="outlined">
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 600 }}>Machine Name</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>User Name</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Action</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Installed At</TableCell>
                                                            <TableCell sx={{ fontWeight: 600 }}>Last Updated</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {versionStat.pCs && versionStat.pCs.length > 0 ? (
                                                            versionStat.pCs.map((pc: PCInstallationDetail, pcIndex: number) => (
                                                                <TableRow key={pcIndex} hover>
                                                                    <TableCell>
                                                                        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                                                                            <ComputerIcon sx={{ fontSize: 18, color: "text.secondary" }} />
                                                                            {pc.machineName}
                                                                        </Box>
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                                                                            <PersonIcon sx={{ fontSize: 18, color: "text.secondary" }} />
                                                                            {pc.userName}
                                                                        </Box>
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Chip
                                                                            label={pc.status}
                                                                            color={getStatusColor(pc.status)}
                                                                            size="small"
                                                                        />
                                                                    </TableCell>
                                                                    <TableCell>
                                                                        <Chip label={pc.action} size="small" variant="outlined" />
                                                                    </TableCell>
                                                                    <TableCell>{FormatUtcTime.formatDateTime(pc.installedAt)}</TableCell>
                                                                    <TableCell>{FormatUtcTime.formatDateTime(pc.lastUpdatedAt)}</TableCell>
                                                                </TableRow>
                                                            ))
                                                        ) : (
                                                            <TableRow>
                                                                <TableCell colSpan={6} align="center">
                                                                    <Typography variant="body2" color="text.secondary">
                                                                        No PC details available
                                                                    </Typography>
                                                                </TableCell>
                                                            </TableRow>
                                                        )}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        </AccordionDetails>
                                    </Accordion>
                                ))}
                            </Stack>
                        ) : (
                            <Box sx={{ textAlign: "center", py: 4 }}>
                                <Typography variant="body1" color="text.secondary">
                                    No version statistics available
                                </Typography>
                            </Box>
                        )}
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleCloseDialog} variant="contained">
                            Close
                        </Button>
                    </DialogActions>
                </Dialog>
            </Box>
        </AdminLayout>
    );
}
