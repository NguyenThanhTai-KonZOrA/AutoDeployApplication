using ClientLauncher.Implement.ViewModels.Response;

namespace ClientLauncher.Implement.Services.Interface
{

    public interface IInstallationService
    {
        Task<InstallationResult> InstallApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UninstallApplicationAsync(string appCode, string userName);
        Task<InstallationResult> UpdateApplicationAsync(string appCode, string userName);
    }
}