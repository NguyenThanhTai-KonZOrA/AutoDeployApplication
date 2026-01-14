export const UserRole = {
    ADMIN: 'Administrator',
    USER: 'user',
    MANAGER: 'Manager',
    COUNTERSTAFF: 'CounterStaff',
    VIEWER: 'Viewer',
} as const;

export type UserRole = typeof UserRole[keyof typeof UserRole];

// System permissions definition
export const Permission = {
    // Admin Registration permissions
    VIEW_ADMIN_DASHBOARD: 'view_admin_dashboard',
    VIEW_ADMIN_CALL: 'view_admin_call',
    VIEW_APPLICATION_MANAGEMENT: 'view_application_management',
    VIEW_CATEGORY_MANAGEMENT: 'view_category_management',
    VIEW_INSTALLATION_LOGS: 'view_installation_logs',
    VIEW_PACKAGE_MANAGEMENT: 'view_package_management',
    VIEW_ICON_MANAGEMENT: 'view_icon_management',
    VIEW_INSTALLATION_REPORTS: 'view_reports',
    VIEW_ROLE_MANAGEMENT: 'view_role_management',
    VIEW_AUDIT_LOGS: 'view_audit_logs',
    VIEW_SYSTEM_SETTINGS: 'view_system_settings',
    VIEW_TICKET_ARCHIVE: 'view_ticket_archive',
    VIEW_EMPLOYEE_MANAGEMENT: 'view_employee_management',
    VIEW_PERMISSION_MANAGEMENT: 'permission_management',
} as const;

export type Permission = typeof Permission[keyof typeof Permission];

/**
 * Role to Permissions mapping
 * Define permissions for each role
 */
export const ROLE_PERMISSIONS: Record<UserRole, Permission[]> = {
    [UserRole.ADMIN]: [
        Permission.VIEW_ADMIN_DASHBOARD,
        Permission.VIEW_APPLICATION_MANAGEMENT,
        Permission.VIEW_CATEGORY_MANAGEMENT,
        Permission.VIEW_PACKAGE_MANAGEMENT,
        Permission.VIEW_ICON_MANAGEMENT,
        Permission.VIEW_INSTALLATION_LOGS,
        Permission.VIEW_INSTALLATION_REPORTS,
        Permission.VIEW_AUDIT_LOGS,
        Permission.VIEW_ROLE_MANAGEMENT,
        Permission.VIEW_PERMISSION_MANAGEMENT,
        Permission.VIEW_EMPLOYEE_MANAGEMENT,
        Permission.VIEW_SYSTEM_SETTINGS,
    ],
    [UserRole.USER]: [
        Permission.VIEW_ADMIN_CALL,
        Permission.VIEW_ADMIN_DASHBOARD,
    ],
    [UserRole.MANAGER]: [
        Permission.VIEW_ADMIN_DASHBOARD,
        Permission.VIEW_APPLICATION_MANAGEMENT,
        Permission.VIEW_CATEGORY_MANAGEMENT,
        Permission.VIEW_PACKAGE_MANAGEMENT,
        Permission.VIEW_ICON_MANAGEMENT,
        Permission.VIEW_INSTALLATION_LOGS,
        Permission.VIEW_INSTALLATION_REPORTS,
        Permission.VIEW_AUDIT_LOGS,
    ],
    [UserRole.COUNTERSTAFF]: [
        Permission.VIEW_ADMIN_CALL,
    ],
    [UserRole.VIEWER]: [
        Permission.VIEW_ADMIN_DASHBOARD
    ],
};

/**
 * Get the highest priority role from an array of roles
 * Priority: Admin > Manager > Counter Staff > User > Viewer
 */
export const getPrimaryRole = (roles: string[] | null): UserRole | null => {
    if (!roles || roles.length === 0) {
        //console.log('⚠️ [getPrimaryRole] No roles provided');
        return null;
    }

    // console.log('🔍 [getPrimaryRole] Input roles:', roles);
    // console.log('🔍 [getPrimaryRole] Checking against:', {
    //     ADMIN: UserRole.ADMIN,
    //     MANAGER: UserRole.MANAGER,
    //     COUNTERSTAFF: UserRole.COUNTERSTAFF,
    //     USER: UserRole.USER
    // });

    // Check in priority order
    if (roles.includes(UserRole.ADMIN)) {
        // console.log('✅ [getPrimaryRole] Primary role = Administrator');
        return UserRole.ADMIN;
    }
    if (roles.includes(UserRole.MANAGER)) {
        // console.log('✅ [getPrimaryRole] Primary role = Manager');
        return UserRole.MANAGER;
    }
    if (roles.includes(UserRole.COUNTERSTAFF)) {
        // console.log('✅ [getPrimaryRole] Primary role = Counter Staff');
        return UserRole.COUNTERSTAFF;
    }
    if (roles.includes(UserRole.USER)) {
        // console.log('✅ [getPrimaryRole] Primary role = User');
        return UserRole.USER;
    }

    // console.warn('⚠️ [getPrimaryRole] No matching role found!');
    return null;
};

/**
 * Get permissions for an array of roles
 * Returns permissions of the highest priority role only
 */
export const getPermissionsForRole = (roles: string[] | null): Permission[] => {
    if (!roles || roles.length === 0) return [];

    // Get the highest priority role
    const primaryRole = getPrimaryRole(roles);
    if (!primaryRole) {
        // console.warn('⚠️ [getPermissionsForRole] No primary role found');
        return [];
    }

    // console.log('🔐 [getPermissionsForRole] Getting permissions for:', primaryRole);
    // Return permissions of only the primary role
    const permissions = ROLE_PERMISSIONS[primaryRole] || [];
    // console.log('🔐 [getPermissionsForRole] Permissions:', permissions);
    return permissions;
};

/**
 * Check if any of the roles has a specific permission
 */
export const hasPermission = (roles: string[] | null, permission: Permission): boolean => {
    const permissions = getPermissionsForRole(roles);
    return permissions.includes(permission);
};

/**
 * Check if any of the roles has any of the specified permissions
 */
export const hasAnyPermission = (roles: string[] | null, permissions: Permission[]): boolean => {
    const userPermissions = getPermissionsForRole(roles);
    return permissions.some(permission => userPermissions.includes(permission));
};

/**
 * Check if any of the roles has all of the specified permissions
 */
export const hasAllPermissions = (roles: string[] | null, permissions: Permission[]): boolean => {
    const userPermissions = getPermissionsForRole(roles);
    return permissions.every(permission => userPermissions.includes(permission));
};
