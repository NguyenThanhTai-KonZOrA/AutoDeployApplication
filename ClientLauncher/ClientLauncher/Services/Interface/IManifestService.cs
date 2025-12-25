using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IManifestService
    {
        /// <summary>
        /// Get manifest from server (generated from database)
        /// </summary>
        Task<ManifestDto?> GetManifestFromServerAsync(string appCode);

        /// <summary>
        /// Download and save manifest file locally
        /// </summary>
        Task<ManifestDto?> DownloadManifestFromServerAsync(string appCode);

        /// <summary>
        /// Get local manifest if exists
        /// </summary>
        Task<ManifestDto?> GetLocalManifestAsync(string appCode);

        /// <summary>
        /// Save manifest to local storage
        /// </summary>
        Task SaveManifestAsync(string appCode, ManifestDto manifest);

        /// <summary>
        /// Check if update is available
        /// </summary>
        Task<bool> IsUpdateAvailableAsync(string appCode, string currentVersion);

        /// <summary>
        /// Get update type (binary, config, both, none)
        /// </summary>
        Task<string> GetUpdateTypeAsync(string appCode);

        /// <summary>
        /// Check if update is forced
        /// </summary>
        Task<bool> IsUpdateForcedAsync(string appCode);
    }
}