using ClientLauncher.Services.Interface;
using IWshRuntimeLibrary;
using System.IO;

namespace ClientLauncher.Services
{
    public class ShortcutService : IShortcutService
    {
        private readonly string _desktopPath;

        public ShortcutService()
        {
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        public bool CreateDesktopShortcut(string appCode, string appName, string launcherPath, string? iconPath = null)
        {
            try
            {
                var shortcutPath = Path.Combine(_desktopPath, $"{appName}.lnk");

                // Create WshShell object
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                // Set shortcut properties
                shortcut.TargetPath = launcherPath;
                shortcut.Arguments = $"--app={appCode}"; // Pass appCode as argument
                shortcut.WorkingDirectory = Path.GetDirectoryName(launcherPath) ?? string.Empty;
                shortcut.Description = $"Launch {appName}";

                // Set icon if provided
                if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
                {
                    shortcut.IconLocation = iconPath;
                }
                else
                {
                    // Use launcher exe icon as default
                    shortcut.IconLocation = $"{launcherPath},0";
                }

                // Save shortcut
                shortcut.Save();

                return System.IO.File.Exists(shortcutPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create shortcut: {ex.Message}");
                return false;
            }
        }

        public bool RemoveDesktopShortcut(string appName)
        {
            try
            {
                var shortcutPath = Path.Combine(_desktopPath, $"{appName}.lnk");

                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool ShortcutExists(string appName)
        {
            var shortcutPath = Path.Combine(_desktopPath, $"{appName}.lnk");
            return System.IO.File.Exists(shortcutPath);
        }
    }
}