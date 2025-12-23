namespace ClientLauncher.Services
{
    public interface IShortcutService
    {
        bool CreateDesktopShortcut(string appCode, string appName, string launcherPath, string? iconPath = null);
        bool RemoveDesktopShortcut(string appName);
        bool ShortcutExists(string appName);
    }
}
