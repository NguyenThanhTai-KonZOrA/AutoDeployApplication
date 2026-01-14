using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace ClientLauncher.Helpers
{
    public static class ProcessHelper
    {
        /// <summary>
        /// Check if any executable files in the application directory are currently running
        /// </summary>
        /// <param name="appCode">Application code</param>
        /// <returns>List of running process names</returns>
        public static List<string> GetRunningProcessesForApp(string appCode)
        {
            var runningProcesses = new List<string>();
            var appPath = Path.Combine(ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps", appCode, "App");

            if (!Directory.Exists(appPath))
            {
                return runningProcesses;
            }

            try
            {
                // Get all .exe files in the app directory
                var exeFiles = Directory.GetFiles(appPath, "*.exe", SearchOption.AllDirectories);

                foreach (var exeFile in exeFiles)
                {
                    var exeFileName = Path.GetFileNameWithoutExtension(exeFile);

                    // Check if any process with this name is running
                    var processes = Process.GetProcessesByName(exeFileName);

                    foreach (var process in processes)
                    {
                        try
                        {
                            // Verify the process is actually running from this directory
                            var processPath = process.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(processPath) &&
                                processPath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
                            {
                                runningProcesses.Add(process.ProcessName);
                            }
                        }
                        catch
                        {
                            // Process may have exited or access denied
                            // Skip and continue
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error accessing directory or processes
                // Return empty list
            }

            return runningProcesses.Distinct().ToList();
        }

        /// <summary>
        /// Check if application is currently running
        /// </summary>
        public static bool IsApplicationRunning(string appCode)
        {
            return GetRunningProcessesForApp(appCode).Any();
        }

        /// <summary>
        /// Try to kill all processes for an application
        /// </summary>
        public static bool TryKillApplicationProcesses(string appCode, out List<string> failedProcesses)
        {
            failedProcesses = new List<string>();
            var appPath = Path.Combine(ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps", appCode, "App");

            if (!Directory.Exists(appPath))
            {
                return true;
            }

            try
            {
                var exeFiles = Directory.GetFiles(appPath, "*.exe", SearchOption.AllDirectories);

                foreach (var exeFile in exeFiles)
                {
                    var exeFileName = Path.GetFileNameWithoutExtension(exeFile);
                    var processes = Process.GetProcessesByName(exeFileName);

                    foreach (var process in processes)
                    {
                        try
                        {
                            var processPath = process.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(processPath) &&
                                processPath.StartsWith(appPath, StringComparison.OrdinalIgnoreCase))
                            {
                                process.Kill();
                                process.WaitForExit(5000); // Wait up to 5 seconds
                            }
                        }
                        catch
                        {
                            failedProcesses.Add(process.ProcessName);
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return failedProcesses.Count == 0;
        }
    }
}