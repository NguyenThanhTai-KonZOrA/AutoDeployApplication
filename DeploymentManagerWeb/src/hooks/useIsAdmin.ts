import { useMemo } from "react";

export function useIsAdmin() {
    const roles = useMemo(() => {
        const savedRoles = localStorage.getItem("userRoles");
        if (!savedRoles) return [];
        try {
            return JSON.parse(savedRoles);
        } catch {
            return [];
        }
    }, []);
    
    return Array.isArray(roles) && roles.includes("Administrator");
}