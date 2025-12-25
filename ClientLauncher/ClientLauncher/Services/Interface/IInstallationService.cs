using ClientLauncher.Models.Response;

namespace ClientLauncher.Services.Interface
{
    public interface IInstallationService
    {
        Task<InstallationResult> InstallApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName);
    }
}