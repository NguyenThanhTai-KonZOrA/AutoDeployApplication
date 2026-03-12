import { useCallback, useEffect, useRef } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { AUTH_TIMEOUTS } from '../constants/timeouts';

/**
 * Hook auto logout user after a period of inactivity
 * Monitors events: mousemove, mousedown, keydown, scroll, touchstart
 */
export const useAutoLogout = () => {
  const { logout, token } = useAuth();
  const timeoutRef = useRef<number | null>(null);
  const lastActivityRef = useRef<number>(Date.now());

  // Reset timer on user activity
  const resetTimer = useCallback(() => {
    // Only reset timer if user is logged in
    if (!token) return;

    lastActivityRef.current = Date.now();

    // Clear timeout
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    // Set new timeout
    timeoutRef.current = setTimeout(async () => {
      console.log(`🕒 Auto logout due to inactivity (${AUTH_TIMEOUTS.IDLE_TIMEOUT / 60000} minutes)`);
      await logout();
    }, AUTH_TIMEOUTS.IDLE_TIMEOUT);
  }, [logout, token]);

  // Set up event listeners for user activity
  const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];

  useEffect(() => {
    // If no token, no need to track
    if (!token) {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
      }
      return;
    }

    // Initialize timer
    resetTimer();

    // Add event listeners
    events.forEach(event => {
      document.addEventListener(event, resetTimer, true);
    });

    // Cleanup
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
      events.forEach(event => {
        document.removeEventListener(event, resetTimer, true);
      });
    };
  }, [resetTimer, token]);

  // Cleanup when component unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return {
    resetTimer,
    getLastActivity: () => lastActivityRef.current,
    getRemainingTime: () => {
      if (!token) return 0;
      const elapsed = Date.now() - lastActivityRef.current;
      return Math.max(0, AUTH_TIMEOUTS.IDLE_TIMEOUT - elapsed);
    }
  };
};
