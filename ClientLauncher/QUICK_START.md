# 🚀 QUICK START GUIDE

## Issue 1: Windows Service

### Install on Single Machine:
```bash
# 1. Navigate to service directory
cd ClientLauncherService

# 2. Right-click → Run as Administrator
install-service.bat

# 3. Verify
sc query ClientLauncherService
# Should show: STATE: RUNNING
```

### Deploy to Multiple Machines:
```powershell
# PowerShell script for bulk deployment
$machines = @("PC001", "PC002", "PC003")  # Add all 300 machines

foreach ($machine in $machines) {
    Write-Host "Deploying to $machine..."
    
    # Copy files
    Copy-Item ".\ClientLauncherService\publish" `
        -Destination "\\$machine\C$\Services\ClientLauncherService" `
        -Recurse -Force
    
    # Install service
    Invoke-Command -ComputerName $machine -ScriptBlock {
        cd "C:\Services\ClientLauncherService"
        .\install-service.bat
    }
    
    Write-Host "$machine completed!" -ForegroundColor Green
}
```

---

## Issue 2: Auto-Update

### Upload New Version:
```http
POST http://YOUR_SERVER:8102/api/update/clientlauncher/upload

Form Data:
- file: ClientLauncher_2.0.0.zip
- version: 2.0.0
- releaseNotes: Your release notes here
```

### Verify:
- Clients will auto-update on next startup
- Check database: `SELECT ClientVersion, COUNT(*) FROM ClientMachines GROUP BY ClientVersion`

---

## Troubleshooting

### Service not starting:
```bash
# Check event logs
eventvwr.msc → Windows Logs → Application

# Verify config
notepad C:\Services\ClientLauncherService\appsettings.json
```

### Update not downloading:
```bash
# Check server path
dir C:\Updates\ClientLauncher_Latest.zip

# Check client logs
C:\Path\To\ClientLauncher\Logs\ClientLauncher_[date].log
```

---

## Done! 🎉

- Service runs 24/7 even when app closed
- Updates rollout automatically to all 300 machines
- Zero manual intervention needed!
