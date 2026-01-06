using ClientLauncher.Models;
using ClientLauncher.Models.Response;

namespace ClientLauncher.Services.Interface
{
    public interface IInstallationService
    {
        Task<InstallationResult> InstallApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName);
        Task<bool> CommitUpdateAsync(string appCode, ManifestDto manifest, string backupPath, string tempAppPath);
        Task<bool> RollbackUpdateAsync(string appCode, string backupPath, string failedVersion);
    }
}