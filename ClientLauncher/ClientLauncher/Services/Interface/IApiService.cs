using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IApiService
    {
        Task<List<ApplicationDto>> GetAllApplicationsAsync();
        Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName);
        Task<InstallationResultDto> UninstallApplicationAsync(string appCode, string userName);
        /// <summary>
        /// Checks LOCAL installation
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<bool> IsApplicationInstalledAsync(string appCode);
        Task<VersionInfoDto?> GetServerVersionAsync(string appCode);
        /// <summary>
        /// Reads LOCAL file to get installed version
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<string?> GetInstalledBinaryVersionAsync(string appCode);
        Task<string?> GetInstalledVersionAsync(string appCode);
        Task NotifyInstallationAsync(string appCode, string version, bool success, TimeSpan duration, string? error = null, string? oldVersion = null, string action = "Install");
    }
}