@echo off
echo ============================================
echo  ClientLauncher Service Uninstallation
echo ============================================
echo.

REM Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script requires administrator privileges
    echo Please run as Administrator
    pause
    exit /b 1
)

echo Stopping service...
sc stop "ClientLauncherService"
timeout /t 3 /nobreak >nul

echo.
echo Removing service...
sc delete "ClientLauncherService"

if %errorlevel% eq 0 (
    echo.
    echo ============================================
    echo  Service uninstalled successfully!
    echo ============================================
) else (
    echo.
    echo ERROR: Failed to uninstall service
)

echo.
pause
