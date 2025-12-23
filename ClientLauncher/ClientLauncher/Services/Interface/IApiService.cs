using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IApiService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();
        Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName);
        Task<InstallationResultDto> UninstallApplicationAsync(string appCode, string userName);
        Task<bool> IsApplicationInstalledAsync(string appCode);
        Task<VersionInfoDto?> GetServerVersionAsync(string appCode);
        Task<string?> GetInstalledVersionAsync(string appCode);
    }
}