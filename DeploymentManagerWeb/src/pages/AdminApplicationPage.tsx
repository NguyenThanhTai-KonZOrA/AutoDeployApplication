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
    Slide,
    Zoom,
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
    FilterListOff as FilterListOffIcon,
    FilterList as FilterListIcon,
    CloudUploadTwoTone as BackupIcon,
    BorderColorTwoTone as BorderColorTwoToneIcon,
    GridViewTwoTone as GridViewRoundedIcon
} from "@mui/icons-material";
import { useState, useEffect, useMemo } from "react";
import AdminLayout from "../components/layout/AdminLayout";
import { applicationService, categoryService, packageManagementService, iconService } from "../services/deploymentManagerService";
import type { ApplicationResponse, CreateApplicationRequest, UpdateApplicationRequest } from "../type/applicationType";
import type { CategoryResponse } from "../type/categoryType";
import type { IconResponse } from "../type/iconType";
import type { ManifestCreateRequest, ManifestResponse } from "../type/manifestType";
import type { ApplicationPackageResponse } from "../type/packageManagementType";
import { useSetPageTitle } from "../hooks/useSetPageTitle";
import { PAGE_TITLES } from "../constants/pageTitles";
import { FormatUtcTime } from "../utils/formatUtcTime";
import { extractErrorMessage } from "../utils/errorHandler";
import React from "react";
import type { TransitionProps } from "@mui/material/transitions";

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

const Transition = React.forwardRef(function Transition(
    props: TransitionProps & {
        children: React.ReactElement<any, any>;
    },
    ref: React.Ref<unknown>,
) {
    return <Zoom
        timeout={{
            appear: 5000,
            enter: 1000,
            exit: 500,
        }}

        ref={ref} {...props} />;
});

export default function AdminApplicationPage() {
    useSetPageTitle(PAGE_TITLES.APPLICATIONS);

    const API_BASE = (window as any)._env_?.API_BASE;
    const [applications, setApplications] = useState<ApplicationResponse[]>([]);
    const [categories, setCategories] = useState<CategoryResponse[]>([]);
    const [icons, setIcons] = useState<IconResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [toggleLoading, setToggleLoading] = useState<number | null>(null);

    // Search and pagination states
    const [searchTerm, setSearchTerm] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage, setItemsPerPage] = useState(10);
    const [snackbar, setSnackbar] = useState({ open: false, message: "", severity: "success" as "success" | "error" | "warning" });

    // filter search
    const [filterStatus, setFilterStatus] = useState<string>("");
    const [filterCategory, setFilterCategory] = useState<string>("");
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
        ConfigMergeStrategy: "ReplaceAll",
        UpdateType: "Binary",
        ForceUpdate: false,
        ReleaseNotes: "",
        IsStable: true,
        PublishedAt: new Date().toISOString().slice(0, 16),
        BinaryFiles: [],
        ConfigFiles: [],
    });

    // ConfigFile form states
    const [configFileForm, setConfigFileForm] = useState({
        name: "",
        updatePolicy: "merge",
        priority: "server"
    });

    // Form states - Package Upload
    const [packageFormData, setPackageFormData] = useState({
        Version: "",
        PackageType: manifestFormData.UpdateType == "Binary" ? "Binary" : "Config",
        ReleaseNotes: "",
        IsStable: true,
        MinimumClientVersion: "",
        PublishImmediately: true,
    });
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [existingPackages, setExistingPackages] = useState<ApplicationPackageResponse[]>([]);

    // Form validation errors
    const [appFormErrors, setAppFormErrors] = useState({
        appCode: false,
        name: false,
        categoryId: false,
        iconUrl: false,
    });

    const [manifestFormErrors, setManifestFormErrors] = useState({
        Version: false,
        BinaryVersion: false,
        BinaryPackage: false,
        //ConfigVersion: false,
        //ConfigPackage: false,
    });

    const [packageFormErrors, setPackageFormErrors] = useState({
        Version: false,
        PackageType: false,
        ReleaseNotes: false,
        MinimumClientVersion: true,
        file: false,
    });

    // Delete confirmation dialog
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [deletingApplication, setDeletingApplication] = useState<ApplicationResponse | null>(null);
    const [deleteLoading, setDeleteLoading] = useState(false);

    // View manifest dialog
    const [viewManifestDialogOpen, setViewManifestDialogOpen] = useState(false);
    const [viewingManifest, setViewingManifest] = useState<ManifestResponse | null>(null);
    const [manifestLoading, setManifestLoading] = useState(false);
    const [editingManifest, setEditingManifest] = useState(false);

    const showSnackbar = (message: string, severity: "success" | "error" | "warning") => {
        setSnackbar({ open: true, message, severity });
    };

    const handleCloseSnackbar = () => {
        setSnackbar({ ...snackbar, open: false });
    };

    const loadApplications = async () => {
        try {
            //setLoading(true);
            setError(null);
            const data = await applicationService.getAllApplications();
            setApplications(data);
            await loadIcons();
        } catch (error: any) {
            console.error("Error loading applications:", error);
            const errorMessage = extractErrorMessage(error, "Failed to load applications data");
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

    const loadIcons = async () => {
        try {
            const data = await iconService.getIconByType(1); // Type 1 for Application icons
            setIcons(data.filter(icon => icon.isActive)); // Only show active icons
        } catch (error: any) {
            console.error("Error loading icons:", error);
            const errorMessage = extractErrorMessage(error, "Failed to load icons");
            showSnackbar(errorMessage, "error");
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
            ConfigMergeStrategy: "ReplaceAll",
            UpdateType: "Binary",
            ForceUpdate: false,
            ReleaseNotes: "",
            IsStable: true,
            BinaryFiles: [],
            ConfigFiles: [],
            PublishedAt: new Date().toISOString().slice(0, 16),
        });
        setPackageFormData({
            Version: "",
            PackageType: manifestFormData.UpdateType == "Binary" ? "Binary" : "Config",
            ReleaseNotes: "",
            IsStable: true,
            MinimumClientVersion: "",
            PublishImmediately: true,
        });
        setSelectedFile(null);
        setAppFormErrors({ appCode: false, name: false, categoryId: false, iconUrl: false });
        setManifestFormErrors({ Version: false, BinaryVersion: false, BinaryPackage: false/*, ConfigVersion: false, ConfigPackage: false*/ });
        setPackageFormErrors({ Version: false, PackageType: false, ReleaseNotes: false, MinimumClientVersion: true, file: false });
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
            Version: application.packageVersion || "",
            PackageType: application.packageType || "Binary",
            ReleaseNotes: application.releaseNotes || "",
            IsStable: application.isStable !== undefined ? application.isStable : true,
            MinimumClientVersion: application.minimumClientVersion || "",
            PublishImmediately: true,
        });
        setSelectedFile(null);
        setExistingPackages([]);

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
                    ConfigMergeStrategy: manifest.configMergeStrategy || "ReplaceAll",
                    UpdateType: manifest.updateType || "Binary",
                    ForceUpdate: manifest.forceUpdate || false,
                    ReleaseNotes: manifest.releaseNotes || "",
                    IsStable: manifest.isStable !== undefined ? manifest.isStable : true,
                    BinaryFiles: manifest.binaryFiles || [],
                    ConfigFiles: manifest.configFiles || [],
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
                ConfigMergeStrategy: "ReplaceAll",
                UpdateType: "Binary",
                ForceUpdate: false,
                ReleaseNotes: "",
                IsStable: true,
                BinaryFiles: [],
                ConfigFiles: [],
                PublishedAt: new Date().toISOString().slice(0, 16),
            });
        }

        // Load existing packages
        try {
            const packages = await packageManagementService.getPackageByApplicationId(application.id);
            setExistingPackages(packages);
            // If there are packages, pre-fill the latest one
            if (packages && packages.length > 0) {
                const latestPackage = packages[0]; // Assuming the first one is the latest
                setPackageFormData({
                    Version: latestPackage.version || "",
                    PackageType: latestPackage.packageType || "Binary",
                    ReleaseNotes: application.releaseNotes || "",
                    IsStable: application.isStable !== undefined ? application.isStable : true,
                    MinimumClientVersion: application.minimumClientVersion || "",
                    PublishImmediately: true,
                });
            }
        } catch (error) {
            console.error("Error loading packages:", error);
            // No packages exist, keep default values
        }

        setDialogOpen(true);
        loadCategories();
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setEditingApplication(null);
        setAppFormErrors({ appCode: false, name: false, categoryId: false, iconUrl: false });
        setManifestFormErrors({ BinaryPackage: false, BinaryVersion: false/*, ConfigPackage: false, ConfigVersion: false*/, Version: false });
        setPackageFormErrors({ file: false, MinimumClientVersion: false, PackageType: false, ReleaseNotes: false, Version: false });
        setTabValue(0);
    };

    const handleAppFormChange = (field: string, value: string | number) => {
        // Validate Application Name: Only allow English letters, numbers, and spaces
        if (field === "name" && typeof value === "string") {
            // Check for Vietnamese characters
            const vietnameseRegex = /[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]/;

            // Check for special characters (anything other than English letters, numbers, and spaces)
            const specialCharRegex = /[^a-zA-Z0-9\s]/;

            if (vietnameseRegex.test(value)) {
                showSnackbar('The application name cannot contain Vietnamese characters', 'warning');
                return; // Don't update the field
            }

            if (specialCharRegex.test(value)) {
                showSnackbar('The application name cannot contain special characters', 'warning');
                return; // Don't update the field
            }
        }

        setAppFormData(prev => {
            const newData = { ...prev, [field]: value };
            // Auto-generate appCode from name (remove spaces) - only in create mode
            if (field === "name" && dialogMode === "create") {
                newData.appCode = String(value).replace(/\s+/g, "");
            }
            return newData;
        });
        // Clear error for this field
        if (field === "appCode" || field === "name" || field === "categoryId") {
            setAppFormErrors(prev => ({ ...prev, [field]: false }));
        }
    };

    const handleManifestFormChange = (field: string, value: string | number | boolean) => {
        setManifestFormData(prev => {
            const newData = { ...prev, [field]: value };
            // Auto-fill BinaryVersion and ConfigVersion when Version changes
            if (field === "Version") {
                newData.BinaryVersion = String(value);
                //newData.ConfigVersion = String(value);
                // Auto-fill BinaryPackage and ConfigPackage with appCode + version
                if (appFormData.appCode) {
                    newData.BinaryPackage = `${appFormData.appCode}_${value}.zip`;
                    //newData.ConfigPackage = `${appFormData.appCode}_${value}.zip`;
                }
                // Sync package version with manifest version
                setPackageFormData(prevPkg => ({ ...prevPkg, Version: String(value) }));
            }

            if (field === "BinaryVersion") {
                // If Update Type is Binary, sync Package Version
                if (newData.UpdateType === "Binary") {
                    setPackageFormData(prevPkg => ({ ...prevPkg, Version: String(value) }));
                }
            }

            if (field === "ConfigVersion") {
                newData.ConfigVersion = String(value);
                if (value) {
                    // Auto-fill ConfigPackage with appCode + ConfigVersion
                    if (appFormData.appCode) {
                        newData.ConfigPackage = `${appFormData.appCode}_config_${value}.zip`;
                    }
                    // If Update Type is Config, sync Package Version
                    if (newData.UpdateType === "Config") {
                        setPackageFormData(prevPkg => ({ ...prevPkg, Version: String(value) }));
                    }
                } else {
                    newData.ConfigPackage = "";
                }
            }

            // Auto-sync Package Type and Version when Update Type changes
            if (field === "UpdateType") {
                const updateType = String(value);
                if (updateType === "Binary") {
                    // Set Package Type to Binary and sync with Binary Version
                    setPackageFormData(prevPkg => ({
                        ...prevPkg,
                        PackageType: "Binary",
                        Version: newData.BinaryVersion
                    }));
                } else if (updateType === "Config") {
                    // Set Package Type to Config and sync with Config Version
                    setPackageFormData(prevPkg => ({
                        ...prevPkg,
                        PackageType: "Config",
                        Version: newData.ConfigVersion
                    }));
                }
            }

            return newData;
        });
        // Clear error for this field
        if (field === "Version" || field === "BinaryVersion" || field === "BinaryPackage" || field === "ConfigVersion" || field === "ConfigPackage") {
            setManifestFormErrors(prev => ({ ...prev, [field]: false }));
        }
    };

    const handlePackageFormChange = (field: string, value: string | boolean) => {
        setPackageFormData(prev => ({ ...prev, [field]: value }));
        // Clear error for this field
        if (field === "Version" || field === "PackageType" || field === "ReleaseNotes" || field === "MinimumClientVersion") {
            setPackageFormErrors(prev => ({ ...prev, [field]: false }));
        }
    };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files[0]) {
            setSelectedFile(event.target.files[0]);
            setPackageFormErrors((prev) => ({ ...prev, file: false }));
        }
    };

    const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
        setTabValue(newValue);
    };

    // ConfigFile handlers
    const handleConfigFileFormChange = (field: string, value: string) => {
        setConfigFileForm(prev => ({ ...prev, [field]: value }));
    };

    const handleAddConfigFile = () => {
        if (!configFileForm.name.trim()) {
            showSnackbar("Config file name is required", "error");
            return;
        }

        // Check if file already exists
        const exists = manifestFormData.ConfigFiles.some(
            (file) => file.name.toLowerCase() === configFileForm.name.trim().toLowerCase()
        );

        if (exists) {
            showSnackbar("Config file with this name already exists", "error");
            return;
        }

        setManifestFormData(prev => ({
            ...prev,
            ConfigFiles: [...prev.ConfigFiles, {
                name: configFileForm.name.trim(),
                updatePolicy: configFileForm.updatePolicy,
                priority: configFileForm.priority
            }]
        }));

        // Reset form
        setConfigFileForm({
            name: "",
            updatePolicy: "merge",
            priority: "server"
        });
    };

    const handleRemoveConfigFile = (index: number) => {
        setManifestFormData(prev => ({
            ...prev,
            ConfigFiles: prev.ConfigFiles.filter((_, i) => i !== index)
        }));
    };

    const validateAppForm = (): boolean => {
        const errors = {
            appCode: !appFormData.appCode.trim(),
            name: !appFormData.name.trim(),
            categoryId: !appFormData.categoryId || appFormData.categoryId === 0,
            iconUrl: !appFormData.iconUrl.trim(),
        };
        setAppFormErrors(errors);

        if (errors.appCode) {
            showSnackbar("Application Code is required", "error");
            setTabValue(0);
            return false;
        }
        if (errors.name) {
            showSnackbar("Application name is required", "error");
            setTabValue(0);
            return false;
        }
        if (errors.categoryId) {
            showSnackbar("Category is required", "error");
            setTabValue(0);
            return false;
        }
        if (errors.iconUrl) {
            showSnackbar("Application Icon is required", "error");
            setTabValue(0);
            return false;
        }
        return true;
    };

    const validateManifestForm = (): boolean => {
        const errors = {
            Version: !manifestFormData.Version.trim(),
            BinaryVersion: !manifestFormData.BinaryVersion.trim(),
            BinaryPackage: !manifestFormData.BinaryPackage.trim(),
            //ConfigVersion: !manifestFormData.ConfigVersion.trim(),
            //ConfigPackage: !manifestFormData.ConfigPackage.trim(),
        };
        setManifestFormErrors(errors);

        if (errors.Version) {
            showSnackbar("Manifest Version is required", "error");
            setTabValue(1);
            return false;
        }
        if (errors.BinaryVersion) {
            showSnackbar("Manifest Binary Version is required", "error");
            setTabValue(1);
            return false;
        }
        if (errors.BinaryPackage) {
            showSnackbar("Manifest Binary Package is required", "error");
            setTabValue(1);
            return false;
        }
        // if (errors.ConfigVersion) {
        //     showSnackbar("Config Version is required", "error");
        //     setTabValue(1);
        //     return false;
        // }
        // if (errors.ConfigPackage) {
        //     showSnackbar("Config Package is required", "error");
        //     setTabValue(1);
        //     return false;
        // }
        return true;
    };

    const validatePackageForm = (): boolean => {
        const errors = {
            Version: !packageFormData.Version.trim(),
            PackageType: !packageFormData.PackageType.trim(),
            ReleaseNotes: !packageFormData.ReleaseNotes.trim(),
            MinimumClientVersion: !packageFormData.MinimumClientVersion.trim(),
            file: !selectedFile,
        };
        setPackageFormErrors(errors);

        if (errors.Version) {
            showSnackbar("Package Version is required", "error");
            setTabValue(2);
            return false;
        }
        if (errors.PackageType) {
            showSnackbar("Package Type is required", "error");
            setTabValue(2);
            return false;
        }
        if (errors.ReleaseNotes) {
            showSnackbar("Release Notes is required", "error");
            setTabValue(2);
            return false;
        }
        // if (errors.MinimumClientVersion) {
        //     showSnackbar("Minimum Client Version is required", "error");
        //     setTabValue(2);
        //     return false;
        // }
        if (errors.file) {
            showSnackbar("Please select a package file", "error");
            setTabValue(2);
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

            if (!validateManifestForm()) {
                setTabValue(1); // Switch to manifest tab
                return;
            }

            if (!validatePackageForm()) {
                setTabValue(2); // Switch to package tab
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
                setTabValue(1);
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
                        formData.append('ReleaseNotes', packageFormData.ReleaseNotes || 'ReleaseNotes not provided');
                        formData.append('IsStable', 'true'.toString());
                        formData.append('MinimumClientVersion', packageFormData.MinimumClientVersion || 'MinimumClientVersion not provided');
                        formData.append('PublishImmediately', 'true'.toString());

                        // Get current user from localStorage
                        const user = 'admin'
                        formData.append('UploadedBy', user || 'Unknown');

                        await packageManagementService.uploadPackage(formData);
                        showSnackbar(`Package uploaded successfully!`, "success");
                    } catch (error: any) {
                        // Handle HTTP error responses (400, 500, etc.)
                        const errorMessage = extractErrorMessage(error, "Error uploading package");
                        showSnackbar(errorMessage, "error");
                        throw error;
                    }
                }

                setApplications(prev => [...prev, createdApp]);
                handleCloseDialog();
                loadApplications(); // Reload to get updated data
            } catch (error: any) {
                console.error("Error Creating application:", error);
                const errorMessage = extractErrorMessage(error, "Error creating application");
                showSnackbar(errorMessage, "error");
                throw error;
            } finally {
                setDialogLoading(false);
            }
        } else {
            // Edit mode - update application, manifest, and optionally upload package
            if (!validateAppForm()) {
                return;
            } if (!editingApplication) return;

            // check if manifest version has changed and need to update new package for this new version
            if (manifestFormData.Version !== editingApplication.manifestBinaryVersion && !selectedFile) {
                // Version has changed, handle accordingly

                setPackageFormData(prev => ({
                    ...prev,
                    Version: manifestFormData.Version
                }));

                setTabValue(2);
                showSnackbar(`Manifest version has changed from ${editingApplication.manifestBinaryVersion} to ${manifestFormData.Version}. You must update the package`, "error");
                return;
            } else if (selectedFile) {
                setPackageFormData(prev => ({
                    ...prev,
                    PackageFile: selectedFile
                }));
            }

            // check if manifest version has changed and need to update new package for this new version
            if (manifestFormData.ConfigVersion !== editingApplication.manifestConfigVersion && !selectedFile) {
                // Version has changed, handle accordingly

                setPackageFormData(prev => ({
                    ...prev,
                    Version: manifestFormData.ConfigVersion,
                    PackageType: "Config"
                }));

                setTabValue(2);
                showSnackbar(`Manifest config version has changed from ${editingApplication.manifestConfigVersion} to ${manifestFormData.ConfigVersion}. You must update the package`, "error");
                return;
            } else if (selectedFile) {
                setPackageFormData(prev => ({
                    ...prev,
                    PackageFile: selectedFile,
                    Version: manifestFormData.ConfigVersion,
                    PackageType: "Config"
                }));
            }

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
                            const errorMessage = extractErrorMessage(createError, "Error creating application");
                            showSnackbar(errorMessage, "error");
                            throw error;
                        }
                    }
                }

                // Step 3: Upload package if file is selected
                if (selectedFile) {
                    try {
                        const formData = new FormData();
                        formData.append('ApplicationId', editingApplication.id.toString());
                        formData.append('Version', packageFormData.Version);
                        formData.append('PackageType', packageFormData.PackageType);
                        formData.append('PackageFile', selectedFile);
                        formData.append('ReleaseNotes', packageFormData.ReleaseNotes ?? 'ReleaseNotes not provided');
                        formData.append('IsStable', 'true'.toString());
                        formData.append('MinimumClientVersion', packageFormData.MinimumClientVersion ?? 'MinimumClientVersion not provided');
                        formData.append('PublishImmediately', 'true'.toString());

                        formData.append('UploadedBy', 'admin'); // Replace with actual user

                        await packageManagementService.uploadPackage(formData);
                        showSnackbar(`Package uploaded successfully!`, "success");
                    } catch (error: any) {
                        const errorMessage = extractErrorMessage(error, "Error uploading package");
                        showSnackbar(errorMessage, "error");
                        throw error;
                    }
                }

                setApplications(prev =>
                    prev.map(app => app.id === result.id ? result : app)
                );
                handleCloseDialog();
                loadApplications(); // Reload to get updated data
            } catch (error: any) {
                const errorMessage = extractErrorMessage(error, "Error when updating application");
                showSnackbar(errorMessage, "error");
                throw error;
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
            const errorMessage = extractErrorMessage(error, "Error deleting application");
            showSnackbar(errorMessage, "error");
            throw error;
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
                    BinaryFiles: viewingManifest.binaryFiles,
                    ConfigFiles: viewingManifest.configFiles,
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
        loadCategories();
    }, []);

    // Search and pagination logic
    const filteredApplications = useMemo(() => {
        return applications.filter(app => {
            // Search term filter
            const searchMatch = !searchTerm ||
                app.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                app.appCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
                app.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
                app.categoryName.toLowerCase().includes(searchTerm.toLowerCase());

            // Status filter
            const statusMatch = !filterStatus ||
                (filterStatus === "active" && app.isActive) ||
                (filterStatus === "inactive" && !app.isActive);

            // Category filter
            const categoryMatch = !filterCategory ||
                app.categoryName.toString() === filterCategory;

            return searchMatch && statusMatch && categoryMatch;
        });
    }, [applications, searchTerm, filterStatus, filterCategory]);

    // Pagination calculations
    const totalPages = Math.ceil(filteredApplications.length / itemsPerPage);
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const currentPageData = filteredApplications.slice(startIndex, endIndex);

    // Reset to first page when search term or items per page changes
    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, itemsPerPage, filterStatus, filterCategory]);

    const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
        setCurrentPage(value);
    };

    const handleItemsPerPageChange = (event: any) => {
        setItemsPerPage(event.target.value);
        setCurrentPage(1);
    };

    const handleClearFilters = () => {
        setSearchTerm("");
        setFilterStatus("");
        setFilterCategory("");
        setCurrentPage(1);
    };

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(event.target.value);
    };

    const getColorChipByCategory = (categoryId: number, categoryName: string) => {

        switch (categoryId) {
            case 1:
                return <Chip label={categoryName} color="success" size="small" />;
            case 2:
                return <Chip label={categoryName} color="secondary" size="small" />;
            case 3:
                return <Chip label={categoryName} color="primary" size="small" />;
            case 4:
                return <Chip label={categoryName} color="warning" size="small" />;
            default:
                return <Chip label={categoryName} color="default" size="small" />;
        }
    }

    const handleToggleStatus = async (applicationId: number) => {
        try {
            setToggleLoading(applicationId);
            const result = await applicationService.changeStatusApplication(applicationId);
            debugger;
            if (result) {
                showSnackbar(`Change Status for Application ${applicationId} Success!`, "success");
                await loadApplications(); // Don't reset pagination when toggling application
            } else {
                showSnackbar("Failed to change application status", "error");
            }
        } catch (error: any) {
            console.error("Error updating application status:", error);

            // Handle HTTP error responses (400, 500, etc.)
            let errorMessage = "Error updating application status";

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
                        <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center' }}>
                            <FilterListIcon sx={{ mr: 1 }} /> Filters
                        </Typography>
                        <Grid container spacing={2} alignItems="center">
                            {/* Row 1: Search and Filters */}
                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
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
                                    <InputLabel>Category</InputLabel>
                                    <Select
                                        value={filterCategory}
                                        label="Category"
                                        onChange={(e) => {
                                            setFilterCategory(e.target.value);
                                            setCurrentPage(1);
                                        }}
                                    >
                                        <MenuItem value="">All Categories</MenuItem>
                                        {categories.map((category) => (
                                            <MenuItem key={category.id} value={category.displayName}>
                                                {category.displayName}
                                            </MenuItem>
                                        ))}
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 2 }}>
                                <FormControl fullWidth size="small">
                                    <Select
                                        value={filterStatus}
                                        onChange={(e) => {
                                            setFilterStatus(e.target.value);
                                            setCurrentPage(1);
                                        }}
                                        displayEmpty
                                    >
                                        <MenuItem value="">
                                            <em>All Status</em>
                                        </MenuItem>
                                        <MenuItem value="active">Active</MenuItem>
                                        <MenuItem value="inactive">Inactive</MenuItem>
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 1 }}>
                                <Tooltip title="Clear all filters">
                                    <Button
                                        variant="outlined"
                                        onClick={handleClearFilters}
                                        disabled={!searchTerm && !filterStatus && !filterCategory}
                                    >
                                        <FilterListOffIcon />
                                    </Button>
                                </Tooltip>
                            </Grid>


                            {/* Row 2: Actions and Stats */}
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


                            <Grid size={{ xs: 12, md: 3 }}>
                                <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                                    Total: {filteredApplications.length} applications
                                    {(searchTerm || filterStatus || filterCategory) && ` (filtered from ${applications.length})`}
                                </Typography>
                                <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mt: 1 }}>
                                    <Chip
                                        label={`Active: ${filteredApplications.filter(app => app.isActive).length}`}
                                        color="success"
                                        size="small"
                                        variant="outlined"
                                    />
                                    <Chip
                                        label={`Inactive: ${filteredApplications.filter(app => !app.isActive).length}`}
                                        color="error"
                                        size="small"
                                        variant="outlined"
                                    />
                                </Box>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 1 }}>
                                <Button
                                    variant="contained"
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
                                    startIcon={<AddIcon />}
                                    onClick={handleOpenCreateDialog}
                                >
                                    Create New
                                </Button>
                            </Grid>
                        </Grid>
                    </CardContent>
                </Card>

                {/* {loading && <LinearProgress sx={{ mb: 2 }} />} */}

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
                                        {/* <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 80 }}>
                                            ID
                                        </TableCell> */}
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }} align="center">App Icon</TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 120 }}>
                                            Application Code
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 180 }}>
                                            Application Name
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 250 }}>
                                            Description
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 150 }}>
                                            Category
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 130 }}>
                                            Latest Version
                                        </TableCell>
                                        <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 130 }}>
                                            Total Versions
                                        </TableCell>
                                        {/* <TableCell align="center" sx={{ fontWeight: 600, borderRight: '1px solid #e0e0e0', minWidth: 100 }}>
                                            Total Installs
                                        </TableCell> */}
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
                                                        {/* <Tooltip title="View manifest">
                                                            <IconButton
                                                                size="small"
                                                                color="info"
                                                                onClick={() => handleViewManifest(application)}
                                                            >
                                                                <VisibilityIcon />
                                                            </IconButton>
                                                        </Tooltip> */}
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
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    <Box
                                                        component="img"
                                                        src={`${API_BASE}${application.iconUrl}`}
                                                        alt={application.name}
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
                                                {/* <TableCell sx={{ borderRight: '1px solid #e0e0e0', fontWeight: 500 }}>
                                                    {application.id}
                                                </TableCell> */}
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
                                                    {getColorChipByCategory(application.categoryId, application.categoryName)}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    {application.latestVersion || '-'}
                                                </TableCell>
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.totalVersions}
                                                        color="info"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell>
                                                {/* <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.totalInstalls}
                                                        color="success"
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                </TableCell> */}
                                                {/* <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Chip
                                                        label={application.isActive ? "Active" : "Inactive"}
                                                        color={application.isActive ? "success" : "error"}
                                                        size="small"
                                                    />
                                                </TableCell> */}
                                                <TableCell align="center" sx={{ borderRight: '1px solid #e0e0e0' }}>
                                                    <Tooltip title={`${application.isActive ? 'Deactivate' : 'Activate'} application`}>
                                                        <span>
                                                            <Switch
                                                                checked={application.isActive}
                                                                onChange={() => handleToggleStatus(application.id)}
                                                                disabled={toggleLoading === application.id}
                                                                color="success"
                                                                size="small"
                                                            />
                                                        </span>
                                                    </Tooltip>
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
                slots={{
                    transition: Transition,
                }}
                keepMounted
                aria-describedby="alert-dialog-slide-description"
                fullWidth
            >
                <DialogTitle>
                    {dialogMode === "create" ? "Create New Application" : "Edit Application"}
                </DialogTitle>
                <DialogContent>
                    <Tabs value={tabValue} onChange={handleTabChange} sx={{ borderBottom: 1, borderColor: 'divider' }}>
                        <Tab label="Application Info" icon={<GridViewRoundedIcon />} />
                        <Tab label={`Manifest ${dialogMode === "create" ? "(Mandatory)" : ""}`} icon={<BorderColorTwoToneIcon />} />
                        <Tab label={`Upload Package ${dialogMode === "create" ? "(Mandatory)" : ""}`} icon={<BackupIcon />} />
                    </Tabs>

                    <TabPanel value={tabValue} index={0}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            <TextField
                                label="Application Name"
                                fullWidth
                                required
                                value={appFormData.name}
                                onChange={(e) => handleAppFormChange("name", e.target.value)}
                                onBlur={() => {
                                    if (!appFormData.name.trim()) {
                                        setAppFormErrors(prev => ({ ...prev, name: true }));
                                    }
                                }}
                                error={appFormErrors.name}
                                disabled={dialogLoading}
                                helperText={appFormErrors.name ? "Application Name is required" : "Please insert only English letters, numbers, and spaces allowed (Max 50 characters)"}
                                inputProps={{ maxLength: 50 }}
                            />
                            <TextField
                                label="Application Code"
                                fullWidth
                                required
                                value={appFormData.appCode}
                                onChange={(e) => handleAppFormChange("appCode", e.target.value)}
                                onBlur={() => {
                                    if (!appFormData.appCode.trim()) {
                                        setAppFormErrors(prev => ({ ...prev, appCode: true }));
                                    }
                                }}
                                InputProps={{ readOnly: true }}
                                error={appFormErrors.appCode}
                                disabled={dialogLoading || dialogMode === "edit"}

                                // autoFocus
                                helperText={appFormErrors.appCode ? "Application Code is required" : "Unique identifier for the application (auto-generated from name)"}
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

                            <FormControl fullWidth required disabled={dialogLoading} error={appFormErrors.iconUrl}>
                                <InputLabel>Icon</InputLabel>
                                <Select
                                    value={appFormData.iconUrl}
                                    label="Icon"
                                    onChange={(e) => handleAppFormChange("iconUrl", e.target.value)}
                                    error={appFormErrors.iconUrl}
                                    onBlur={() => {
                                        if (!appFormData.iconUrl.trim() || appFormData.iconUrl === "") {
                                            setAppFormErrors(prev => ({ ...prev, iconUrl: true }));
                                        }
                                    }}
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
                                    <MenuItem value="" disabled>
                                        <em>Select an Icon</em>
                                    </MenuItem>
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
                                {appFormErrors.iconUrl && (
                                    <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1.75 }}>
                                        Icon is required
                                    </Typography>
                                )}
                            </FormControl>

                            <FormControl fullWidth required disabled={dialogLoading} error={appFormErrors.categoryId}>
                                <InputLabel>Category</InputLabel>
                                <Select
                                    label="Category"
                                    value={appFormData.categoryId}
                                    onChange={(e) => handleAppFormChange("categoryId", e.target.value)}
                                    onBlur={() => {
                                        if (!appFormData.categoryId || appFormData.categoryId === 0) {
                                            setAppFormErrors(prev => ({ ...prev, categoryId: true }));
                                        }
                                    }}
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
                                {appFormErrors.categoryId && (
                                    <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1.75 }}>
                                        Category is required
                                    </Typography>
                                )}
                            </FormControl>
                        </Box>
                    </TabPanel>

                    <TabPanel value={tabValue} index={1}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            {dialogMode === "create" && (
                                <Alert severity="info" sx={{ mb: 1 }}>
                                    Fill out this section to create an initial manifest for the application.
                                </Alert>
                            )}
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12 }}>
                                    <TextField
                                        label="Version"
                                        fullWidth
                                        required
                                        value={manifestFormData.Version}
                                        onChange={(e) => handleManifestFormChange("Version", e.target.value)}
                                        onBlur={() => {
                                            if (!manifestFormData.Version.trim()) {
                                                setManifestFormErrors(prev => ({ ...prev, Version: true }));
                                            }
                                        }}
                                        error={manifestFormErrors.Version}
                                        disabled={dialogLoading}
                                        helperText={manifestFormErrors.Version ? "Version is required" : "e.g., 1.0.0 (auto-fills Binary/Config versions)"}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Binary Version"
                                        fullWidth
                                        required
                                        value={manifestFormData.BinaryVersion}
                                        onChange={(e) => handleManifestFormChange("BinaryVersion", e.target.value)}
                                        onBlur={() => {
                                            if (!manifestFormData.BinaryVersion.trim()) {
                                                setManifestFormErrors(prev => ({ ...prev, BinaryVersion: true }));
                                            }
                                        }}
                                        error={manifestFormErrors.BinaryVersion}
                                        disabled={dialogLoading || manifestFormData.UpdateType === "Config"}
                                        helperText={manifestFormErrors.BinaryVersion ? "Binary Version is required" : "Auto-filled from Version"}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Binary Package"
                                        fullWidth
                                        required
                                        value={manifestFormData.BinaryPackage}
                                        onChange={(e) => handleManifestFormChange("BinaryPackage", e.target.value)}
                                        onBlur={() => {
                                            if (!manifestFormData.BinaryPackage.trim()) {
                                                setManifestFormErrors(prev => ({ ...prev, BinaryPackage: true }));
                                            }
                                        }}
                                        error={manifestFormErrors.BinaryPackage}
                                        disabled={dialogLoading || manifestFormData.BinaryPackage !== ""}
                                        helperText={manifestFormErrors.BinaryPackage ? "Binary Package is required" : "Auto-filled: AppCode_Version"}
                                    />
                                </Grid>

                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <FormControl fullWidth disabled={dialogLoading}>
                                        <InputLabel>Update Type</InputLabel>
                                        <Select
                                            label="Update Type"
                                            value={manifestFormData.UpdateType}
                                            onChange={(e) => handleManifestFormChange("UpdateType", e.target.value)}
                                        >
                                            <MenuItem value="Binary">Binary</MenuItem>
                                            <MenuItem value="Config" disabled={dialogMode === "create"}>Config</MenuItem>
                                            {/* <MenuItem value="Both" disabled={dialogMode === "create"}>Both</MenuItem> */}
                                        </Select>
                                    </FormControl>
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
                                        {/* <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={manifestFormData.IsStable}
                                                    onChange={(e) => handleManifestFormChange("IsStable", e.target.checked)}
                                                    disabled={dialogLoading}
                                                />
                                            }
                                            label="Is Stable"
                                        /> */}
                                    </Box>
                                </Grid>

                                {/* Config Files Management Section */}
                                <Grid size={{ xs: 12, sm: 6 }} sx={{ display: dialogMode === "edit" ? "block" : "none" }}>
                                    <TextField
                                        label="Config Version"
                                        fullWidth
                                        //required
                                        value={manifestFormData.ConfigVersion}
                                        onChange={(e) => handleManifestFormChange("ConfigVersion", e.target.value)}
                                        onBlur={() => {
                                            if (!manifestFormData.ConfigVersion.trim()) {
                                                setManifestFormErrors(prev => ({ ...prev, ConfigVersion: true }));
                                            }
                                        }}
                                        //error={manifestFormErrors.ConfigVersion}
                                        disabled={dialogLoading || manifestFormData.UpdateType === "Binary"}
                                    //helperText={manifestFormErrors.ConfigVersion ? "Config Version is required" : "Auto-filled from Version"}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }} sx={{ display: dialogMode === "edit" ? "block" : "none" }}>
                                    <TextField
                                        label="Config Package"
                                        fullWidth
                                        //required
                                        value={manifestFormData.ConfigPackage}
                                        onChange={(e) => handleManifestFormChange("ConfigPackage", e.target.value)}
                                        onBlur={() => {
                                            if (!manifestFormData.ConfigPackage.trim()) {
                                                setManifestFormErrors(prev => ({ ...prev, ConfigPackage: true }));
                                            }
                                        }}
                                        //error={manifestFormErrors.ConfigPackage}
                                        disabled={dialogLoading || manifestFormData.ConfigPackage !== "" || manifestFormData.UpdateType === "Binary"}
                                    //helperText={manifestFormErrors.ConfigPackage ? "Config Package is required" : "Auto-filled: AppCode_Version"}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }} sx={{ display: dialogMode === "edit" ? "block" : "none" }}>
                                    <FormControl fullWidth disabled={dialogLoading}>
                                        <InputLabel>Config Merge Strategy</InputLabel>
                                        <Select
                                            label="Config Merge Strategy"
                                            value={manifestFormData.ConfigMergeStrategy}
                                            onChange={(e) => handleManifestFormChange("ConfigMergeStrategy", e.target.value)}
                                            disabled={dialogLoading || manifestFormData.UpdateType === "Binary"}
                                        >
                                            <MenuItem value="ReplaceAll">Replace All</MenuItem>
                                            <MenuItem value="Selective">Selective</MenuItem>
                                            <MenuItem value="PreserveLocal">Preserve Local</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Grid>


                                {/* <Grid size={{ xs: 12 }} sx={{ display: dialogMode === "edit" ? "block" : "none" }}>
                                    <Box sx={{ mt: 2, mb: 1 }}>
                                        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
                                            Config Files
                                        </Typography>
                                        <Typography variant="caption" color="text.secondary">
                                            Manage configuration file update policies and priorities
                                        </Typography>
                                    </Box>
                                </Grid>

                                <Grid size={{ xs: 12 }} sx={{ display: dialogMode === "edit" ? "block" : "none" }}>
                                    <Paper variant="outlined" sx={{ p: 2, bgcolor: 'grey.50' }}>
                                        <Typography variant="body2" fontWeight={600} sx={{ mb: 2 }}>
                                            Add Config File
                                        </Typography>
                                        <Grid container spacing={2}>
                                            <Grid size={{ xs: 12, sm: 6 }}>
                                                <TextField
                                                    label="File Name"
                                                    fullWidth
                                                    size="small"
                                                    placeholder="e.g., appsettings.json"
                                                    value={configFileForm.name}
                                                    onChange={(e) => handleConfigFileFormChange("name", e.target.value)}
                                                    disabled={dialogLoading}
                                                    helperText="Please provide a config file name exactly. Many thanks!"
                                                    required={manifestFormData.ConfigPackage !== ""}
                                                    error={manifestFormData.ConfigPackage !== "" && configFileForm.name.trim() === "" && dialogMode === "create"}
                                                />
                                            </Grid>
                                            <Grid size={{ xs: 12, sm: 3 }}>
                                                <FormControl fullWidth size="small" disabled={dialogLoading}>
                                                    <InputLabel>Update Policy</InputLabel>
                                                    <Select
                                                        label="Update Policy"
                                                        value={configFileForm.updatePolicy}
                                                        onChange={(e) => handleConfigFileFormChange("updatePolicy", e.target.value)}
                                                    >
                                                        <MenuItem value="merge">Merge</MenuItem>
                                                        <MenuItem value="replace">Replace</MenuItem>
                                                        <MenuItem value="preserve">Preserve</MenuItem>
                                                    </Select>
                                                </FormControl>
                                            </Grid>
                                            <Grid size={{ xs: 12, sm: 3 }}>
                                                <FormControl fullWidth size="small" disabled={dialogLoading}>
                                                    <InputLabel>Priority</InputLabel>
                                                    <Select
                                                        label="Priority"
                                                        value={configFileForm.priority}
                                                        onChange={(e) => handleConfigFileFormChange("priority", e.target.value)}
                                                    >
                                                        <MenuItem value="server">Server</MenuItem>
                                                        <MenuItem value="local">Local</MenuItem>
                                                    </Select>
                                                </FormControl>
                                            </Grid>
                                            <Grid size={{ xs: 12 }}>
                                                <Button
                                                    variant="outlined"
                                                    startIcon={<AddIcon />}
                                                    onClick={handleAddConfigFile}
                                                    disabled={dialogLoading}
                                                    size="small"
                                                >
                                                    Add Config File
                                                </Button>
                                            </Grid>
                                        </Grid>
                                    </Paper>
                                </Grid> */}

                                {/* Config Files List */}
                                {manifestFormData.ConfigFiles.length > 0 && (
                                    <Grid size={{ xs: 12 }}>
                                        <TableContainer component={Paper} variant="outlined">
                                            <Table size="small">
                                                <TableHead>
                                                    <TableRow sx={{ bgcolor: 'grey.100' }}>
                                                        <TableCell sx={{ fontWeight: 600 }}>File Name</TableCell>
                                                        <TableCell sx={{ fontWeight: 600 }}>Update Policy</TableCell>
                                                        <TableCell sx={{ fontWeight: 600 }}>Priority</TableCell>
                                                        <TableCell align="center" sx={{ fontWeight: 600 }}>Action</TableCell>
                                                    </TableRow>
                                                </TableHead>
                                                <TableBody>
                                                    {manifestFormData.ConfigFiles.map((file, index) => (
                                                        <TableRow key={index}>
                                                            <TableCell>{file.name}</TableCell>
                                                            <TableCell>
                                                                <Chip
                                                                    label={file.updatePolicy}
                                                                    size="small"
                                                                    color={
                                                                        file.updatePolicy === 'merge' ? 'primary' :
                                                                            file.updatePolicy === 'replace' ? 'warning' : 'success'
                                                                    }
                                                                    variant="outlined"
                                                                />
                                                            </TableCell>
                                                            <TableCell>
                                                                <Chip
                                                                    label={file.priority}
                                                                    size="small"
                                                                    color={file.priority === 'server' ? 'info' : 'secondary'}
                                                                    variant="outlined"
                                                                />
                                                            </TableCell>
                                                            <TableCell align="center">
                                                                <IconButton
                                                                    size="small"
                                                                    color="error"
                                                                    onClick={() => handleRemoveConfigFile(index)}
                                                                    disabled={dialogLoading}
                                                                >
                                                                    <DeleteIcon fontSize="small" />
                                                                </IconButton>
                                                            </TableCell>
                                                        </TableRow>
                                                    ))}
                                                </TableBody>
                                            </Table>
                                        </TableContainer>
                                    </Grid>
                                )}

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
                                {/* <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Published At"
                                        type="datetime-local"
                                        fullWidth
                                        value={manifestFormData.PublishedAt}
                                        onChange={(e) => handleManifestFormChange("PublishedAt", e.target.value)}
                                        disabled={dialogLoading}
                                        InputLabelProps={{ shrink: true }}
                                    />
                                </Grid> */}

                            </Grid>
                        </Box>
                    </TabPanel>

                    <TabPanel value={tabValue} index={2}>
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            {dialogMode === "create" && (
                                <Alert severity="info" sx={{ mb: 1 }}>
                                    Upload a package file for this application.
                                </Alert>
                            )}
                            {/* Existing Packages Section - Only show in Edit mode */}
                            {dialogMode === "edit" && existingPackages.length > 0 && (
                                <Box sx={{ mb: 2 }}>
                                    <Typography variant="subtitle1" fontWeight={600} sx={{ mb: 1 }}>
                                        Existing Packages
                                    </Typography>
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
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {existingPackages.map((pkg) => (
                                                    <TableRow key={pkg.id}>
                                                        <TableCell>
                                                            <Chip label={pkg.version} size="small" color="primary" variant="outlined" />
                                                        </TableCell>
                                                        <TableCell>{pkg.packageType}</TableCell>
                                                        <TableCell>{pkg.packageFileName}</TableCell>
                                                        <TableCell>{pkg.fileSizeFormatted}</TableCell>
                                                        <TableCell>{pkg.downloadCount}</TableCell>
                                                        <TableCell>
                                                            {pkg.lastDownloadedAt ? FormatUtcTime.formatDateTime(pkg.lastDownloadedAt) : 'Never'}
                                                        </TableCell>
                                                    </TableRow>
                                                ))}
                                            </TableBody>
                                        </Table>
                                    </TableContainer>
                                    <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                                        Upload a new package to add a new version
                                    </Typography>
                                </Box>
                            )}

                            {/* Upload New Package Form */}
                            <Typography variant="subtitle1" fontWeight={600} sx={{ mt: 2 }}>
                                {dialogMode === "edit" ? "Upload New Package Version" : "Upload Package"}
                            </Typography>

                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Package Version"
                                        fullWidth
                                        required
                                        value={packageFormData.Version}
                                        onChange={(e) => handlePackageFormChange("Version", e.target.value)}
                                        onBlur={() => {
                                            if (!packageFormData.Version.trim()) {
                                                setPackageFormErrors(prev => ({ ...prev, Version: true }));
                                            }
                                        }}
                                        error={packageFormErrors.Version}
                                        disabled={dialogLoading || dialogMode === "create"}
                                        helperText={packageFormErrors.Version ? "Package Version is required" : "Synced with Manifest Version"}
                                    />
                                </Grid>
                                <Grid size={{ xs: 12, sm: 6 }}>
                                    <FormControl fullWidth disabled={dialogLoading} required error={packageFormErrors.PackageType}>
                                        <InputLabel>Package Type</InputLabel>
                                        <Select
                                            label="Package Type"
                                            value={packageFormData.PackageType}
                                            onChange={(e) => handlePackageFormChange("PackageType", e.target.value as string)}
                                            onBlur={() => {
                                                if (!packageFormData.PackageType.trim()) {
                                                    setPackageFormErrors(prev => ({ ...prev, PackageType: true }));
                                                }
                                            }}
                                        >
                                            <MenuItem value="Binary">Binary</MenuItem>
                                            <MenuItem value="Config" disabled={dialogMode === "create"}>Config</MenuItem>
                                            {/* <MenuItem value="Full">Full Package</MenuItem> */}
                                        </Select>
                                        {packageFormErrors.PackageType && (
                                            <Typography variant="caption" color="error" sx={{ mt: 0.5, ml: 1.75 }}>
                                                Package Type is required
                                            </Typography>
                                        )}
                                    </FormControl>
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <Button
                                        variant="outlined"
                                        component="label"
                                        fullWidth
                                        startIcon={<AttachFileIcon />}
                                        disabled={dialogLoading}
                                        sx={{
                                            py: 2,
                                            borderColor: packageFormErrors.file ? 'error.main' : undefined,
                                            color: packageFormErrors.file ? 'error.main' : undefined,
                                        }}
                                    >
                                        {selectedFile ? selectedFile.name : "Choose Package File *"}
                                        <input
                                            type="file"
                                            hidden
                                            onChange={handleFileChange}
                                            accept=".zip"
                                        />
                                    </Button>
                                    {selectedFile ? (
                                        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                                            File size: {(selectedFile.size / 1024 / 1024).toFixed(2)} MB
                                        </Typography>
                                    ) : packageFormErrors.file ? (
                                        <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block', ml: 1.75 }}>
                                            Package file is required
                                        </Typography>
                                    ) : null}
                                </Grid>
                                <Grid size={{ xs: 12 }}>
                                    <TextField
                                        label="Release Notes"
                                        required
                                        fullWidth
                                        multiline
                                        rows={3}
                                        value={packageFormData.ReleaseNotes}
                                        onChange={(e) => handlePackageFormChange("ReleaseNotes", e.target.value)}
                                        onBlur={() => {
                                            if (!packageFormData.ReleaseNotes.trim()) {
                                                setPackageFormErrors(prev => ({ ...prev, ReleaseNotes: true }));
                                            }
                                        }}
                                        error={packageFormErrors.ReleaseNotes}
                                        disabled={dialogLoading}
                                        helperText={packageFormErrors.ReleaseNotes ? "Release Notes is required" : ""}
                                    />
                                </Grid>
                                {/* <Grid size={{ xs: 12, sm: 6 }}>
                                    <TextField
                                        label="Minimum Client Version"
                                        fullWidth
                                        required
                                        value={packageFormData.MinimumClientVersion}
                                        onChange={(e) => handlePackageFormChange("MinimumClientVersion", e.target.value)}
                                        onBlur={() => {
                                            if (!packageFormData.MinimumClientVersion.trim()) {
                                                setPackageFormErrors(prev => ({ ...prev, MinimumClientVersion: true }));
                                            }
                                        }}
                                        error={packageFormErrors.MinimumClientVersion}
                                        disabled={dialogLoading}
                                        helperText={packageFormErrors.MinimumClientVersion ? "Minimum Client Version is required" : "Minimum version required to install"}
                                    />
                                </Grid> */}
                                {/* <Grid size={{ xs: 12, sm: 6 }}>
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
                                </Grid> */}
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
                                            disabled={dialogLoading || manifestFormData.UpdateType === "Config"}
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
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.configVersion || '-'}</Typography>
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
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.configPackage || '-'}</Typography>
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
                                                <MenuItem value="ReplaceAll">Replace All</MenuItem>
                                                <MenuItem value="Selective">Selective</MenuItem>
                                                <MenuItem value="PreserveLocal">Preserve Local</MenuItem>
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

                                {/* Config Files Section */}
                                {viewingManifest.configFiles && viewingManifest.configFiles.length > 0 && (
                                    <Grid size={{ xs: 12 }}>
                                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>Config Files</Typography>
                                        <TableContainer component={Paper} variant="outlined">
                                            <Table size="small">
                                                <TableHead>
                                                    <TableRow sx={{ bgcolor: 'grey.100' }}>
                                                        <TableCell sx={{ fontWeight: 600 }}>File Name</TableCell>
                                                        <TableCell sx={{ fontWeight: 600 }}>Update Policy</TableCell>
                                                        <TableCell sx={{ fontWeight: 600 }}>Priority</TableCell>
                                                    </TableRow>
                                                </TableHead>
                                                <TableBody>
                                                    {viewingManifest.configFiles.map((file, index) => (
                                                        <TableRow key={index}>
                                                            <TableCell>{file.name}</TableCell>
                                                            <TableCell>
                                                                <Chip
                                                                    label={file.updatePolicy}
                                                                    size="small"
                                                                    color={
                                                                        file.updatePolicy === 'merge' ? 'primary' :
                                                                            file.updatePolicy === 'replace' ? 'warning' : 'success'
                                                                    }
                                                                    variant="outlined"
                                                                />
                                                            </TableCell>
                                                            <TableCell>
                                                                <Chip
                                                                    label={file.priority}
                                                                    size="small"
                                                                    color={file.priority === 'server' ? 'info' : 'secondary'}
                                                                    variant="outlined"
                                                                />
                                                            </TableCell>
                                                        </TableRow>
                                                    ))}
                                                </TableBody>
                                            </Table>
                                        </TableContainer>
                                    </Grid>
                                )}

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
                                        <Typography variant="body1" fontWeight={500}>{viewingManifest.releaseNotes || '-'}</Typography>
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
