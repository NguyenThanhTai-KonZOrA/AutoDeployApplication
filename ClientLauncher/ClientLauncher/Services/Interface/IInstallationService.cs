using ClientLauncher.Models;
using ClientLauncher.Models.Response;

public interface IInstallationService
{
    Task<InstallationResult> InstallApplicationAsync(string appCode, string userName);
    Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName);
    Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName);
    Task<bool> CommitUpdateAsync(string appCode, ManifestDto manifest, string backupPath, string tempAppPath);
    Task<bool> RollbackUpdateAsync(string appCode, string backupPath, string failedVersion);
    Task<bool> FinalizeUpdateAsync(string appCode, string backupPath);
    Task NotifyInstallationAsync(
             string appCode,
             string version,
             bool success,
             TimeSpan duration,
             string? error = null,
             string? oldVersion = null,
             string action = "Install");

    string? GetVersionFromBackup(string backupPath);
}