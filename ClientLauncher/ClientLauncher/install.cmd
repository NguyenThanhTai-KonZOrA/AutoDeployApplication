@echo off
echo ====================================
echo ClientLauncher Installation
echo ====================================

:: Kiểm tra admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Error: This installer requires Administrator privileges.
    echo Please run as Administrator.
    pause
    exit /b 1
)

:: Cài ClientLauncher vào Program Files (x86)
set INSTALL_DIR=%ProgramFiles(x86)%\ClientLauncher

echo Installing to: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy files
echo Copying files...
xcopy /E /I /Y ".\publish\*" "%INSTALL_DIR%\"

:: Tạo thư mục CompanyApps (cho app data - KHÔNG trong Program Files)
echo Creating application data directory...
if not exist "C:\CompanyApps" (
    mkdir "C:\CompanyApps"
    :: Cấp quyền write cho Users
    icacls "C:\CompanyApps" /grant Users:(OI)(CI)F /T
)

:: Tạo Start Menu shortcut
echo Creating Start Menu shortcut...
set SHORTCUT_DIR=%ProgramData%\Microsoft\Windows\Start Menu\Programs\ClientLauncher
if not exist "%SHORTCUT_DIR%" mkdir "%SHORTCUT_DIR%"

powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_DIR%\ClientLauncher.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\ClientLauncher.exe'; $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\Icons\app_default.ico'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Save()"

:: Tạo Desktop shortcut cho All Users
echo Creating Desktop shortcut...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%PUBLIC%\Desktop\ClientLauncher.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\ClientLauncher.exe'; $Shortcut.IconLocation = '%INSTALL_DIR%\Assets\Icons\app_default.ico'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Save()"

:: Tạo Uninstaller
echo Creating uninstaller...
(
echo @echo off
echo echo Uninstalling ClientLauncher...
echo rd /s /q "%INSTALL_DIR%"
echo del "%PUBLIC%\Desktop\ClientLauncher.lnk"
echo rd /s /q "%SHORTCUT_DIR%"
echo echo.
echo echo ClientLauncher has been uninstalled.
echo echo.
echo echo Note: Application data in C:\CompanyApps was not removed.
echo pause
) > "%INSTALL_DIR%\Uninstall.cmd"

:: Thêm vào Add/Remove Programs
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /v DisplayName /t REG_SZ /d "ClientLauncher" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /v UninstallString /t REG_SZ /d "\"%INSTALL_DIR%\Uninstall.cmd\"" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /v DisplayIcon /t REG_SZ /d "%INSTALL_DIR%\Assets\Icons\app_default.ico" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /v Publisher /t REG_SZ /d "Your Company Name" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClientLauncher" /v DisplayVersion /t REG_SZ /d "1.0.0" /f

echo.
echo ====================================
echo Installation completed successfully!
echo ====================================
echo.
echo ClientLauncher installed to: %INSTALL_DIR%
echo Application data folder: C:\CompanyApps
echo Shortcuts created in Start Menu and Desktop
echo.
pause