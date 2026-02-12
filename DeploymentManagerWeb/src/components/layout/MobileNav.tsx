import * as React from 'react';
import { useLocation, Link as RouterLink } from 'react-router-dom';
import {
    Box,
    Drawer,
    Stack,
    List,
    ListItem,
    ListItemButton,
    ListItemIcon,
    ListItemText,
} from '@mui/material';
import {
    Dashboard as DashboardIcon,
    Assignment as AssignmentIcon,
    People as PeopleIcon,
    Settings as SettingsIcon,
    Assessment as AssessmentIcon,
    BarChart as BarChartIcon,
    PersonSearch as PersonSearchIcon,
    DesignServices as DesignServicesIcon,
    Groups as GroupsIcon,
    AddBusiness as AddBusinessIcon,
    FormatListNumberedRtl as FormatListNumberedRtlIcon,
    ManageHistory as ManageHistoryIcon,
    AssignmentInd as RoleManagementIcon,
    VerifiedUser as PermissionManagementIcon,
    Archive as ArchiveIcon,
    Apps as AppIcon,
    Category as CategoryIcon,
    FileDownloadDone as FileDownloadDoneIcon,
    Filter9Plus as Filter9PlusIcon,
} from '@mui/icons-material';
import { Permission } from '../../constants/roles';
import { usePermission } from '../../hooks/usePermission';
import type { NavItem } from '../../type/commonType';

const navItems: NavItem[] = [
    {
        key: 'dashboard',
        title: 'Dashboard',
        href: '/admin-dashboard',
        icon: DashboardIcon,
        requiredPermission: Permission.VIEW_ADMIN_DASHBOARD
    },
    {
        key: 'application',
        title: 'Application Management',
        href: '/admin-application',
        icon: AppIcon,
        requiredPermission: Permission.VIEW_APPLICATION_MANAGEMENT
    },
    {
        key: 'category',
        title: 'Category Management',
        href: '/admin-category',
        icon: CategoryIcon,
        requiredPermission: Permission.VIEW_CATEGORY_MANAGEMENT
    },
    {
        key: 'package',
        title: 'Packages Management',
        href: '/admin-package',
        icon: FileDownloadDoneIcon,
        requiredPermission: Permission.VIEW_PACKAGE_MANAGEMENT
    },
    {
        key: 'icons',
        title: 'Icons Management',
        href: '/admin-icons',
        icon: Filter9PlusIcon,
        requiredPermission: Permission.VIEW_ICON_MANAGEMENT
    },
    {
        key: 'installation',
        title: 'Installation Logs',
        href: '/admin-installation',
        icon: ManageHistoryIcon,
        requiredPermission: Permission.VIEW_INSTALLATION_LOGS
    },
    {
        key: 'report-by-application',
        title: 'Installation Report',
        href: '/admin-report-by-application',
        icon: AssessmentIcon,
        requiredPermission: Permission.VIEW_INSTALLATION_REPORTS
    },
    {
        key: 'admin-audit-logs',
        title: 'Audit Logs',
        href: '/admin-audit-logs',
        icon: ManageHistoryIcon,
        requiredPermission: Permission.VIEW_AUDIT_LOGS,
    },
    {
        key: 'admin-roles',
        title: 'Role Management',
        href: '/admin-roles',
        icon: RoleManagementIcon,
        requiredPermission: Permission.VIEW_ROLE_MANAGEMENT
    },
    {
        key: 'admin-permissions',
        title: 'Permission Management',
        href: '/admin-permissions',
        icon: PermissionManagementIcon,
        requiredPermission: Permission.VIEW_PERMISSION_MANAGEMENT
    },
    {
        key: 'admin-employees',
        title: 'Employee Management',
        href: '/admin-employees',
        icon: PeopleIcon,
        requiredPermission: Permission.VIEW_EMPLOYEE_MANAGEMENT
    },
    {
        key: 'admin-settings',
        title: 'System Settings',
        href: '/admin-settings',
        icon: SettingsIcon,
        requiredPermission: Permission.VIEW_SYSTEM_SETTINGS
    },
];

interface MobileNavProps {
    onClose: () => void;
    open: boolean;
}

export function MobileNav({ onClose, open }: MobileNavProps): React.JSX.Element {
    const location = useLocation();
    // ← Added hook usePermission
    const { can } = usePermission();

    // Filter nav items base on permissions
    const filteredNavItems = navItems.filter((item) => {
        // If item does not require permission, always show
        if (!item.requiredPermission) {
            return true;
        }
        // Only show if user has permission
        return can(item.requiredPermission);
    });

    return (
        <Drawer
            anchor="left"
            onClose={onClose}
            open={open}
            PaperProps={{
                sx: {
                    bgcolor: '#274549',
                    color: 'var(--mui-palette-common-white)',
                    width: '320px',
                },
            }}
            sx={{ display: { lg: 'none' } }}
        >
            <Stack spacing={2} sx={{ p: 1 }}>
                <Box sx={{ display: 'inline-center', justifyContent: 'center', alignItems: 'center' }}>
                    <Box sx={{ display: 'inline-center', justifyContent: 'center', alignItems: 'center' }}>
                        <img src="/images/TheGrandHoTram.png" alt="Logo" style={{ height: 90, width: 'auto' }} />
                    </Box>
                </Box>
            </Stack>

            <Stack spacing={1} sx={{ p: 1 }}>
                <Box sx={{ display: 'inline-center', justifyContent: 'center', alignItems: 'center' }}>
                    <Box sx={{ display: 'inline-center', justifyContent: 'center', alignItems: 'center', color: 'white', fontSize: 18, fontWeight: 'bold' }}>
                        Deployment Manager
                    </Box>
                </Box>
            </Stack>

            <Box
                sx={{
                    borderTop: '1px solid rgba(255, 255, 255, 0.1)',
                    flex: '1 1 auto',
                    overflow: 'auto'
                }}
            >
                <List sx={{ p: 1 }}>
                    {filteredNavItems.map((item) => {
                        const Icon = item.icon;
                        const isActive = location.pathname === item.href;

                        return (
                            <ListItem key={item.key} disablePadding sx={{ mb: 0.5 }}>
                                <ListItemButton
                                    component={RouterLink}
                                    to={item.href}
                                    onClick={onClose}
                                    sx={{
                                        borderRadius: 1,
                                        color: isActive ? 'white' : 'rgba(255, 255, 255, 0.7)',
                                        backgroundColor: isActive ? 'var(--mui-palette-primary-main)' : 'transparent',
                                        '&:hover': {
                                            backgroundColor: isActive
                                                ? 'var(--mui-palette-primary-dark)'
                                                : 'rgba(255, 255, 255, 0.04)',
                                        },
                                        py: 1,
                                        px: 2,
                                    }}
                                >
                                    <ListItemIcon
                                        sx={{
                                            color: 'inherit',
                                            minWidth: 40,
                                        }}
                                    >
                                        <Icon fontSize="small" />
                                    </ListItemIcon>
                                    <ListItemText
                                        primary={item.title}
                                        primaryTypographyProps={{
                                            fontSize: '0.875rem',
                                            fontWeight: 500,
                                        }}
                                    />
                                </ListItemButton>
                            </ListItem>
                        );
                    })}
                </List>
            </Box>
        </Drawer>
    );
}
