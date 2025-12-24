using ClientLauncher.Models.Response;

namespace ClientLauncher.Services.Interface
{
    public interface IInstallationService
    {
        Task<InstallationResult> InstallApplicationAsync(string appCode, string packageName);
        Task<InstallationResult> UpdateApplicationAsync(string appCode, string packageName);
        Task<bool> UninstallApplicationAsync(string appCode);
    }
}