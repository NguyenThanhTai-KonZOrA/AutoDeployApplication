namespace ClientLauncher.Services.Interface
{
    public interface IInstallationChecker
    {
        bool IsApplicationInstalled(string appCode);
        string? GetInstalledVersion(string appCode);
        string? GetInstalledBinaryVersion(string appCode);
        string? GetInstalledConfigVersion(string appCode);
    }
}