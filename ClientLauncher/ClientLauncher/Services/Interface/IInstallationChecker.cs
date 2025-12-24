namespace ClientLauncher.Services.Interface
{
    public interface IInstallationChecker
    {
        bool IsApplicationInstalled(string appCode);
        string? GetInstalledVersion(string appCode);
        bool HasManifest(string appCode);
    }
}