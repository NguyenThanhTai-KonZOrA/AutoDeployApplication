import { useEffect, useRef } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { AUTH_TIMEOUTS } from '../constants/timeouts';

/**
 * Hook to periodically validate token
 * Checks token validity every 5 minutes
 * Forces logout if token becomes invalid
 */
export const useTokenValidator = () => {
    const { token, validateAndRefreshToken } = useAuth();
    const intervalRef = useRef<number | null>(null);

    useEffect(() => {
        // Only run if user is logged in
        if (!token) {
            return;
        }

        console.log('🔐 Starting periodic token validation...');

        // Validate immediately on mount
        validateAndRefreshToken();

        // Then validate every 5 minutes
        intervalRef.current = window.setInterval(async () => {
            console.log('🔍 Periodic token validation check...');
            const isValid = await validateAndRefreshToken();

            if (!isValid) {
                console.log('❌ Token validation failed, user will be logged out');
            }
        }, AUTH_TIMEOUTS.TOKEN_VALIDATION_INTERVAL); // 10 minutes

        // Cleanup on unmount
        return () => {
            if (intervalRef.current) {
                console.log('🧹 Clearing token validation interval');
                clearInterval(intervalRef.current);
            }
        };
    }, [token, validateAndRefreshToken]);
};
