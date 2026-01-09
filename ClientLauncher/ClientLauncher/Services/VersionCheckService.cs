using ClientLauncher.Services.Interface;
using NLog;

namespace ClientLauncher.Services
{
    public class VersionCheckService : IVersionCheckService
    {
        private readonly IManifestService _manifestService;
        private readonly IInstallationChecker _installationChecker;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public VersionCheckService()
        {
            _manifestService = new ManifestService();
            _installationChecker = new InstallationChecker();
        }

        public async Task<bool> IsUpdateAvailableAsync(string appCode)
        {
            try
            {
                Logger.Info("Checking for updates: {AppCode}", appCode);

                // 1. Get local version
                var localVersion = _installationChecker.GetInstalledVersion(appCode);
                if (string.IsNullOrEmpty(localVersion))
                {
                    Logger.Debug("No local version found for {AppCode}", appCode);
                    return false;
                }

                // 2. Check against server manifest (generated from database)
                var isUpdateAvailable = await _manifestService.IsUpdateAvailableAsync(appCode, localVersion);

                Logger.Info("Update check result for {AppCode}: {Result}", appCode, isUpdateAvailable);
                return isUpdateAvailable;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking for updates: {AppCode}", appCode);
                return false;
            }
        }

        public async Task<bool> IsUpdateConfigAvailableAsync(string appCode)
        {
            try
            {
                Logger.Info("Checking for updates: {AppCode}", appCode);

                // 1. Get local version
                var localVersion = _installationChecker.GetInstalledConfigVersion(appCode);
                if (string.IsNullOrEmpty(localVersion))
                {
                    Logger.Debug("No local version found for {AppCode}", appCode);
                    return false;
                }

                // 2. Check against server manifest (generated from database)
                var isUpdateAvailable = await _manifestService.IsUpdateConfigAvailableAsync(appCode, localVersion);

                Logger.Info("Update check result for {AppCode}: {Result}", appCode, isUpdateAvailable);
                return isUpdateAvailable;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking for updates: {AppCode}", appCode);
                return false;
            }
        }

        public async Task<string?> GetLatestVersionAsync(string appCode)
        {
            try
            {
                var manifest = await _manifestService.GetManifestFromServerAsync(appCode);
                var version = manifest?.Binary?.Version;

                Logger.Debug("Latest version for {AppCode}: {Version}", appCode, version ?? "N/A");
                return version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting latest version for {AppCode}", appCode);
                return null;
            }
        }

        public async Task<bool> IsForceUpdateRequiredAsync(string appCode)
        {
            try
            {
                var isForced = await _manifestService.IsUpdateForcedAsync(appCode);
                Logger.Debug("Force update check for {AppCode}: {IsForced}", appCode, isForced);
                return isForced;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking force update for {AppCode}", appCode);
                return false;
            }
        }
    }
}