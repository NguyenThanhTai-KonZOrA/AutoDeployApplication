import React from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { UserRole } from "../constants/roles";

/**
 * Component to redirect users to different pages based on their role
 * Admin -> Dashboard
 * Manager -> Dashboard  
 * Counter Staff -> Counter Terminal
 * User -> Counter Terminal
 */
const RoleBasedRedirect: React.FC = () => {
    const { roles } = useAuth();

    // Redirect based on highest priority role
    if (roles.includes(UserRole.ADMIN)) {
        return <Navigate to="/admin-dashboard" replace />;
    } else if (roles.includes(UserRole.MANAGER)) {
        return <Navigate to="/admin-dashboard" replace />;
    } else if (roles.includes(UserRole.COUNTERSTAFF)) {
        return <Navigate to="/admin-application" replace />;
    } else if (roles.includes(UserRole.USER)) {
        return <Navigate to="/admin-dashboard" replace />;
    } else {
        // Default fallback for unknown roles
        return <Navigate to="/admin-application" replace />;
    }
};

export default RoleBasedRedirect;
