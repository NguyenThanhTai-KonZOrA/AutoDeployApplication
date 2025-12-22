using ClientLauncher.Models;

namespace ClientLauncherAPI.Services.Interface
{
    public interface IVersionService
    {
        LocalAppInfo GetLocalVersions(string appCode);
        bool IsNewerVersion(string serverVersion, string localVersion);
        void SaveBinaryVersion(string appCode, string version);
        void SaveConfigVersion(string appCode, string version);
    }

    
}