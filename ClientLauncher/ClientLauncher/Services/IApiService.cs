using ClientLauncher.Models;

namespace ClientLauncher.Services
{
    public interface IApiService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();
        Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName);
        Task<bool> IsApplicationInstalledAsync(string appCode);
    }
}