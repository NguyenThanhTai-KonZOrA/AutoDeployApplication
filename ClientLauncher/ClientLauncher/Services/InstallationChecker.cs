using ClientLauncher.Services.Interface;
using NLog;
using System.IO;

namespace ClientLauncher.Services
{
    public class InstallationChecker : IInstallationChecker
    {
        private readonly string _appBasePath;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public InstallationChecker()
        {
            _appBasePath = @"C:\CompanyApps";
            Logger.Debug("InstallationChecker initialized with base path: {Path}", _appBasePath);
        }

        public bool IsApplicationInstalled(string appCode)
        {
            try
            {
                var versionFilePath = Path.Combine(_appBasePath, $"{appCode}/App", "version.txt");
                var appFolderPath = Path.Combine(_appBasePath, appCode, "App");

                bool versionFileExists = File.Exists(versionFilePath);
                bool appFolderExists = Directory.Exists(appFolderPath);
                bool hasExecutable = false;

                if (appFolderExists)
                {
                    hasExecutable = Directory.GetFiles(appFolderPath, "*.exe", SearchOption.TopDirectoryOnly).Any();
                }

                bool isInstalled = versionFileExists && appFolderExists && hasExecutable;

                Logger.Info("Installation check for {AppCode}: " +
                    "VersionFile={VersionFile}, AppFolder={AppFolder}, HasExe={HasExe}, IsInstalled={IsInstalled}",
                    appCode, versionFileExists, appFolderExists, hasExecutable, isInstalled);

                if (!isInstalled)
                {
                    Logger.Debug("Installation paths checked:\n" +
                        "  Version file: {VersionFile}\n" +
                        "  App folder: {AppFolder}",
                        versionFilePath, appFolderPath);
                }

                return isInstalled;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error checking installation for {AppCode}", appCode);
                return false;
            }
        }

        public string? GetInstalledVersion(string appCode)
        {
            try
            {
                var versionFilePath = Path.Combine(_appBasePath, $"{appCode}/App", "version.txt");

                if (!File.Exists(versionFilePath))
                {
                    Logger.Debug("Version file not found for {AppCode} at {Path}", appCode, versionFilePath);
                    return null;
                }

                var version = File.ReadAllText(versionFilePath).Trim();
                Logger.Debug("Installed version for {AppCode}: {Version}", appCode, version);
                return version;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reading version for {AppCode}", appCode);
                return null;
            }
        }
    }
}