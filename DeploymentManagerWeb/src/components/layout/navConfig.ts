
import {
    Dashboard as DashboardIcon,
    People as PeopleIcon,
    Settings as SettingsIcon,
    Assessment as AssessmentIcon,
    ManageHistory as ManageHistoryIcon,
    AssignmentInd as RoleManagementIcon,
    VerifiedUser as PermissionManagementIcon,
    Apps as AppIcon,
    Category as CategoryIcon,
    FileDownloadDone as FileDownloadDoneIcon,
    Filter9Plus as Filter9PlusIcon,
    Security as SecurityIcon,
    InstallDesktop as InstallDesktopIcon,
} from '@mui/icons-material';
import { Permission } from '../../constants/roles';
import type { NavItem } from '../../type/commonType';

export const navItems: NavItem[] = [
    {
        key: 'dashboard',
        title: 'Dashboard',
        href: '/admin-dashboard',
        icon: DashboardIcon,
        requiredPermission: Permission.VIEW_ADMIN_DASHBOARD
    },
    {
        key: 'management',
        title: 'Management',
        icon: AssessmentIcon,
        children: [
            {
                key: 'application',
                title: 'Application',
                href: '/admin-application',
                icon: AppIcon,
                requiredPermission: Permission.VIEW_APPLICATION_MANAGEMENT
            },
            {
                key: 'package',
                title: 'Packages',
                href: '/admin-package',
                icon: FileDownloadDoneIcon,
                requiredPermission: Permission.VIEW_PACKAGE_MANAGEMENT
            },
            {
                key: 'category',
                title: 'Category',
                href: '/admin-category',
                icon: CategoryIcon,
                requiredPermission: Permission.VIEW_CATEGORY_MANAGEMENT
            },
            {
                key: 'icons',
                title: 'Icons',
                href: '/admin-icons',
                icon: Filter9PlusIcon,
                requiredPermission: Permission.VIEW_ICON_MANAGEMENT
            }]
    },
    {
        key: 'reports-and-logs',
        title: 'Reports & Logs',
        icon: ManageHistoryIcon,
        children: [
            {
                key: 'report-by-application',
                title: 'Installation Report',
                href: '/admin-report-by-application',
                icon: AssessmentIcon,
                requiredPermission: Permission.VIEW_INSTALLATION_REPORTS
            },
            {
                key: 'installation',
                title: 'Installation Logs',
                href: '/admin-installation',
                icon: InstallDesktopIcon,
                requiredPermission: Permission.VIEW_INSTALLATION_LOGS
            },
            {
                key: 'admin-audit-logs',
                title: 'Audit Logs',
                href: '/admin-audit-logs',
                icon: ManageHistoryIcon,
                requiredPermission: Permission.VIEW_AUDIT_LOGS,
            },]
    },
    {
        key: 'role-permission',
        title: 'Role & Permission',
        icon: SecurityIcon,
        children: [
            {
                key: 'admin-roles',
                title: 'Role',
                href: '/admin-roles',
                icon: RoleManagementIcon,
                requiredPermission: Permission.VIEW_ROLE_MANAGEMENT
            },
            {
                key: 'admin-permissions',
                title: 'Permission',
                href: '/admin-permissions',
                icon: PermissionManagementIcon,
                requiredPermission: Permission.VIEW_PERMISSION_MANAGEMENT
            },
            {
                key: 'admin-employees',
                title: 'Employee',
                href: '/admin-employees',
                icon: PeopleIcon,
                requiredPermission: Permission.VIEW_EMPLOYEE_MANAGEMENT
            },
        ]
    },
    {
        key: 'admin-settings',
        title: 'System Settings',
        href: '/admin-settings',
        icon: SettingsIcon,
        requiredPermission: Permission.VIEW_SYSTEM_SETTINGS
    },
];