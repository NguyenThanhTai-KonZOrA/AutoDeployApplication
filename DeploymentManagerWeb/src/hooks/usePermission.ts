import { useAuth } from '../contexts/AuthContext';
import { Permission, hasPermission, hasAnyPermission, hasAllPermissions, UserRole, getPrimaryRole as getPrimaryRoleUtil } from '../constants/roles';

/**
 * Hook check permissions of the current user
 * Note: All permission checks are based on the HIGHEST PRIORITY role only
 */
export const usePermission = () => {
    const { roles } = useAuth();

    /**
     * Check if user has a specific permission (based on primary role only)
     */
    const can = (permission: Permission): boolean => {
        return hasPermission(roles, permission);
    };

    /**
     * Check if user has any of the specified permissions (based on primary role only)
     */
    const canAny = (permissions: Permission[]): boolean => {
        return hasAnyPermission(roles, permissions);
    };

    /**
     * Check if user has all of the specified permissions (based on primary role only)
     */
    const canAll = (permissions: Permission[]): boolean => {
        return hasAllPermissions(roles, permissions);
    };

    /**
     * Check if user is admin (primary role is Administrator)
     */
    const isAdmin = (): boolean => {
        return roles.includes(UserRole.ADMIN);
    };

    /**
     * Check if user has a specific role in their roles array
     */
    const hasRole = (requiredRole: UserRole): boolean => {
        return roles.includes(requiredRole);
    };

    /**
     * Get the highest priority role (Admin > Manager > Counter Staff > User)
     */
    const getPrimaryRole = (): UserRole | null => {
        return getPrimaryRoleUtil(roles);
    };

    return {
        can,
        canAny,
        canAll,
        isAdmin,
        hasRole,
        getPrimaryRole,
        roles,
    };
};
