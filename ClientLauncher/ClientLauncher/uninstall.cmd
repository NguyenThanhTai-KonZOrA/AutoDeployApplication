@echo off
setlocal enabledelayedexpansion

:: ====================================
:: ClientLauncher Uninstaller
:: Version: 1.0.0
:: ====================================

color 0E
title ClientLauncher Uninstaller

echo.
echo ====================================
echo   ClientLauncher Uninstaller
echo ====================================
echo.

:: Kiểm tra admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    color 0C
    echo [ERROR] This uninstaller requires Administrator privileges.
    echo.
    echo Please right-click this file and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo [INFO] Administrator privileges verified.
echo.

set INSTALL_DIR=%ProgramFiles(x86)%\ClientLauncher
set APPS_DATA_DIR=C:\CompanyApps
set SHORTCUT_DIR=%ProgramData%\Microsoft\Windows\Start Menu\Programs\ClientLauncher

echo [WARNING] This will remove ClientLauncher from your system.
echo.
echo Installation directory: %INSTALL_DIR%
echo.

choice /C YN /M "Are you sure you want to uninstall ClientLauncher"
if errorlevel 2 (
    echo.
    echo Uninstallation cancelled.
    pause
    exit /b 0
)

echo.
echo [INFO] Starting uninstallation process...
echo.

:: Đóng ứng dụng đang chạy
echo [STEP 1/5] Closing running instances...
taskkill /F /IM ClientLauncher.exe >nul 2>&1
timeout /t 2 /nobreak >nul
echo [OK] Application closed.
echo.

:: Xóa Desktop shortcut
echo [STEP 2/5] Removing Desktop shortcut...
if exist "%PUBLIC%\Desktop\ClientLauncher.lnk" (
    del /F /Q "%PUBLIC%\Desktop\ClientLauncher.lnk" >nul 2>&1
    echo [OK] Desktop shortcut removed.
) else (
    echo [INFO] Desktop shortcut not found.
)
echo.

:: Xóa Start Menu shortcut
echo [STEP 3/5] Removing Start Menu shortcut...
if exist "%SHORTCUT_DIR%" (
    rd /S /Q "%SHORTCUT_DIR%" >nul 2>&1
    echo [OK] Start Menu shortcut removed.
) else (
    echo [INFO] Start Menu shortcut not found.
)
echo.

:: Xóa registry entries
echo [STEP 4/5] Removing registry entries...
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /f >nul 2>&1
if errorlevel 1 (
    echo [WARNING] Failed to remove registry entries
) else (
    echo [OK] Registry entries removed.
)
echo.

:: Hỏi có xóa application data không
echo [STEP 5/5] Application data cleanup...
echo.
echo The following folder contains application data:
echo   %APPS_DATA_DIR%
echo.

choice /C YN /M "Do you want to remove application data (all installed apps will be deleted)"
if errorlevel 2 (
    echo [INFO] Application data preserved at: %APPS_DATA_DIR%
) else (
    echo [INFO] Removing application data...
    if exist "%APPS_DATA_DIR%" (
        rd /S /Q "%APPS_DATA_DIR%" >nul 2>&1
        if errorlevel 1 (
            echo [WARNING] Failed to remove some application data
        ) else (
            echo [OK] Application data removed.
        )
    )
)
echo.

:: Xóa program files (phải làm cuối cùng vì script đang chạy từ đây)
echo [INFO] Removing program files...
echo.

:: Tạo script tự xóa (vì không thể xóa chính mình khi đang chạy)
set TEMP_UNINSTALL=%TEMP%\ClientLauncher_cleanup.cmd
(
    echo @echo off
    echo timeout /t 2 /nobreak ^>nul
    echo rd /S /Q "%INSTALL_DIR%" ^>nul 2^>^&1
    echo if exist "%INSTALL_DIR%" ^(
    echo     echo [WARNING] Some files could not be removed. Please delete manually:
    echo     echo   %INSTALL_DIR%
    echo     pause
    echo ^) else ^(
    echo     echo [OK] Program files removed successfully.
    echo ^)
    echo del /F /Q "%TEMP_UNINSTALL%" ^>nul 2^>^&1
) > "%TEMP_UNINSTALL%"

:: Hoàn thành
color 0A
echo.
echo ====================================
echo   Uninstallation Completed!
echo ====================================
echo.
echo ClientLauncher has been removed from your system.
echo.
echo Thank you for using ClientLauncher!
echo.
echo ====================================
echo.
pause

:: Chạy cleanup script và thoát
start /min cmd /c "%TEMP_UNINSTALL%"
exit /b 0