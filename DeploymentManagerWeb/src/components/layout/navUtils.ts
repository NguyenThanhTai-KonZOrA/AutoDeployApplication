import type { NavItem } from '../../type/commonType';
import type { Permission } from '../../constants/roles';
import { logError } from '../../utils/errorHandler';

/**
 * Filter navigation items based on user permissions
 * @param items - Array of navigation items to filter
 * @param canAccess - Function to check if user has access to a permission
 * @returns Filtered array of navigation items
 */
export function filterNavItems(
    items: NavItem[],
    canAccess: (permission: Permission) => boolean
): NavItem[] {
    return items.filter((item) => {
        // For items with children, filter children first
        if (item.children) {
            const filteredChildren = filterNavItems(item.children, canAccess);
            // Only show group if it has at least one accessible child
            return filteredChildren.length > 0;
        }

        // If item does not require permission, always show
        if (!item.requiredPermission) {
            return true;
        }

        // Only show if user has permission
        return canAccess(item.requiredPermission);
    }).map(item => {
        // If item has children, return with filtered children
        if (item.children) {
            return {
                ...item,
                children: filterNavItems(item.children, canAccess)
            };
        }
        return item;
    });
}

/**
 * Load navigation groups open state from localStorage
 * @param storageKey - Key to use for localStorage
 * @returns Object with group keys and their open state
 */
export function loadNavGroupsState(storageKey: string): Record<string, boolean> {
    try {
        const saved = localStorage.getItem(storageKey);
        return saved ? JSON.parse(saved) : {};
    } catch {
        return {};
    }
}

/**
 * Save navigation groups open state to localStorage
 * @param storageKey - Key to use for localStorage
 * @param state - Object with group keys and their open state
 */
export function saveNavGroupsState(storageKey: string, state: Record<string, boolean>): void {
    try {
        localStorage.setItem(storageKey, JSON.stringify(state));
    } catch (error) {
        logError('navUtils.saveState', error);
    }
}
