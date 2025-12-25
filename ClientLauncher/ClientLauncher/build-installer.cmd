@echo off
setlocal enabledelayedexpansion

:: ====================================
:: ClientLauncher Build & Package Script
:: ====================================

color 0B
title Building ClientLauncher Installer Package

echo.
echo ====================================
echo   ClientLauncher Build Script
echo ====================================
echo.

:: Configuration
set PROJECT_NAME=ClientLauncher
set PROJECT_FILE=ClientLauncher.csproj
set CONFIGURATION=Release
set RUNTIME=win-x64
set FRAMEWORK=net8.0-windows
set OUTPUT_DIR=.\publish
set PACKAGE_DIR=.\installer-package
set VERSION=1.0.0

echo [CONFIG] Project: %PROJECT_NAME%
echo [CONFIG] Configuration: %CONFIGURATION%
echo [CONFIG] Runtime: %RUNTIME%
echo [CONFIG] Framework: %FRAMEWORK%
echo.

:: Kiểm tra project file
if not exist "%PROJECT_FILE%" (
    color 0C
    echo [ERROR] Project file not found: %PROJECT_FILE%
    echo.
    echo Please run this script from the ClientLauncher project directory.
    pause
    exit /b 1
)

:: Xóa thư mục publish cũ
if exist "%OUTPUT_DIR%" (
    echo [INFO] Cleaning previous build...
    rd /S /Q "%OUTPUT_DIR%" >nul 2>&1
)

:: Build & Publish
echo.
echo [STEP 1/4] Building project...
echo.

dotnet publish "%PROJECT_FILE%" ^
    --configuration %CONFIGURATION% ^
    --runtime %RUNTIME% ^
    --framework %FRAMEWORK% ^
    --self-contained true ^
    --output "%OUTPUT_DIR%" ^
    /p:PublishSingleFile=false ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    color 0C
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [OK] Build completed successfully.
echo.

:: Kiểm tra output
if not exist "%OUTPUT_DIR%\ClientLauncher.exe" (
    color 0C
    echo [ERROR] Build output not found: %OUTPUT_DIR%\ClientLauncher.exe
    pause
    exit /b 1
)

:: Tạo installer package
echo [STEP 2/4] Creating installer package...
echo.

if exist "%PACKAGE_DIR%" (
    rd /S /Q "%PACKAGE_DIR%" >nul 2>&1
)
mkdir "%PACKAGE_DIR%"

:: Copy publish files
xcopy /E /I /Y /Q "%OUTPUT_DIR%\*" "%PACKAGE_DIR%\publish\" >nul

:: Copy installer scripts
copy /Y "install.cmd" "%PACKAGE_DIR%\" >nul 2>&1
copy /Y "uninstall.cmd" "%PACKAGE_DIR%\" >nul 2>&1

:: Tạo README
(
    echo ClientLauncher Installation Package
    echo =====================================
    echo.
    echo Version: %VERSION%
    echo Built: %DATE% %TIME%
    echo.
    echo INSTALLATION INSTRUCTIONS:
    echo.
    echo 1. Right-click 'install.cmd'
    echo 2. Select "Run as Administrator"
    echo 3. Follow the on-screen instructions
    echo.
    echo UNINSTALLATION:
    echo.
    echo - Use "Add or Remove Programs" in Windows Settings
    echo - Or right-click 'Uninstall.cmd' and "Run as Administrator"
    echo.
    echo SUPPORT:
    echo.
    echo For support, please contact: support@yourcompany.com
    echo.
) > "%PACKAGE_DIR%\README.txt"

echo [OK] Installer package created.
echo.

:: Tạo ZIP archive
echo [STEP 3/4] Creating ZIP archive...
echo.

set ZIP_NAME=%PROJECT_NAME%-v%VERSION%-Installer.zip

if exist "%ZIP_NAME%" del /F /Q "%ZIP_NAME%" >nul 2>&1

powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%PACKAGE_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force" >nul 2>&1

if errorlevel 1 (
    echo [WARNING] Failed to create ZIP archive
) else (
    echo [OK] ZIP archive created: %ZIP_NAME%
)
echo.

:: Hiển thị kết quả
echo [STEP 4/4] Build summary...
echo.

:: Tính size
for %%A in ("%OUTPUT_DIR%") do set SIZE=%%~zA
set /a SIZE_MB=!SIZE! / 1048576

echo ====================================
echo   Build Completed Successfully!
echo ====================================
echo.
echo Output:
echo   - Build directory: %OUTPUT_DIR%
echo   - Package directory: %PACKAGE_DIR%
echo   - ZIP archive: %ZIP_NAME%
echo.
echo Package size: ~%SIZE_MB% MB
echo.
echo Files included:
dir /B "%PACKAGE_DIR%"
echo.
echo ====================================
echo.
echo Next steps:
echo   1. Test the installation:
echo      - Right-click '%PACKAGE_DIR%\install.cmd'
echo      - Run as Administrator
echo.
echo   2. Distribute the installer:
echo      - Share the '%ZIP_NAME%' file
echo      - Or share the '%PACKAGE_DIR%' folder
echo.
echo ====================================
echo.

choice /C YN /M "Do you want to open the package directory"
if errorlevel 2 goto :end
if errorlevel 1 (
    start "" "%PACKAGE_DIR%"
)

:end
pause
exit /b 0