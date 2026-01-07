using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IApiService
    {
        /// <summary>
        /// GetAllApplicationsAsync
        /// </summary>
        /// <returns></returns>
        Task<List<ApplicationDto>> GetAllApplicationsAsync();
        /// <summary>
        /// InstallApplicationAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<InstallationResultDto> InstallApplicationAsync(string appCode, string userName);
        /// <summary>
        /// UninstallApplicationAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<InstallationResultDto> UninstallApplicationAsync(string appCode, string userName);
        /// <summary>
        /// Checks LOCAL installation
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<bool> IsApplicationInstalledAsync(string appCode);
        /// <summary>
        /// GetServerVersionAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<VersionInfoDto?> GetServerVersionAsync(string appCode);
        /// <summary>
        /// Reads LOCAL file to get installed version
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<string?> GetInstalledBinaryVersionAsync(string appCode);
        /// <summary>
        /// GetInstalledVersionAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        Task<string?> GetInstalledVersionAsync(string appCode);
        /// <summary>
        /// NotifyInstallationAsync
        /// </summary>
        /// <param name="appCode"></param>
        /// <param name="version"></param>
        /// <param name="success"></param>
        /// <param name="duration"></param>
        /// <param name="error"></param>
        /// <param name="oldVersion"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        Task NotifyInstallationAsync(string appCode, string version, bool success, TimeSpan duration, string? error = null, string? oldVersion = null, string action = "Install");
    }
}