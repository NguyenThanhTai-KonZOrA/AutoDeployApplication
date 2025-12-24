using ClientLauncher.Services.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLauncher.Services
{
    public class InstallationChecker : IInstallationChecker
    {
        private readonly string _appsBasePath;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public InstallationChecker()
        {
            _appsBasePath = System.Configuration.ConfigurationManager.AppSettings["AppsBasePath"]
                ?? @"C:\CompanyApps";
        }

        /// <summary>
        /// Check if application is installed on LOCAL machine
        /// </summary>
        public bool IsApplicationInstalled(string appCode)
        {
            try
            {
                // Check 1: Manifest exists
                var manifestPath = Path.Combine(_appsBasePath, appCode, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    _logger.Debug("Manifest not found for {AppCode} at {Path}", appCode, manifestPath);
                    return false;
                }

                // Check 2: App folder exists
                var appPath = Path.Combine(_appsBasePath, appCode, "App");
                if (!Directory.Exists(appPath))
                {
                    _logger.Debug("App folder not found for {AppCode} at {Path}", appCode, appPath);
                    return false;
                }

                // Check 3: At least one .exe file exists
                var exeFiles = Directory.GetFiles(appPath, "*.exe", SearchOption.TopDirectoryOnly);
                if (exeFiles.Length == 0)
                {
                    _logger.Debug("No executable found for {AppCode} in {Path}", appCode, appPath);
                    return false;
                }

                _logger.Info("Application {AppCode} is installed at {Path}", appCode, appPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking installation status for {AppCode}", appCode);
                return false;
            }
        }

        /// <summary>
        /// Get installed version from LOCAL version.txt file
        /// </summary>
        public string? GetInstalledVersion(string appCode)
        {
            try
            {
                var versionFile = Path.Combine(_appsBasePath, appCode, "App", "version.txt");

                if (!File.Exists(versionFile))
                {
                    _logger.Debug("Version file not found for {AppCode}", appCode);
                    return null;
                }

                var version = File.ReadAllText(versionFile).Trim();
                _logger.Debug("Installed version for {AppCode}: {Version}", appCode, version);
                return version;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error reading version for {AppCode}", appCode);
                return null;
            }
        }

        /// <summary>
        /// Check if manifest exists (shortcut created but app not downloaded yet)
        /// </summary>
        public bool HasManifest(string appCode)
        {
            try
            {
                var manifestPath = Path.Combine(_appsBasePath, appCode, "manifest.json");
                return File.Exists(manifestPath);
            }
            catch
            {
                return false;
            }
        }
    }
}