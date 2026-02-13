@echo off
echo ============================================
echo  ClientLauncher Service Installation
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

echo Building service...
dotnet publish -c Release -o .\publish

if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Installing service...
sc create "ClientLauncherService" binPath= "%~dp0publish\ClientLauncherService.exe" start= auto DisplayName= "ClientLauncher Deployment Service"

if %errorlevel% neq 0 (
    echo ERROR: Service installation failed
    pause
    exit /b 1
)

echo.
echo Configuring service description...
sc description "ClientLauncherService" "Automatic deployment service for ClientLauncher applications. Handles remote deployment tasks and machine registration."

echo.
echo Starting service...
sc start "ClientLauncherService"

echo.
echo ============================================
echo  Installation completed successfully!
echo ============================================
echo.
echo Service Name: ClientLauncherService
echo Display Name: ClientLauncher Deployment Service
echo Status: Running
echo Startup Type: Automatic
echo.
echo You can manage this service from:
echo - Services.msc (services console)
echo - Task Manager ^> Services tab
echo.
pause
