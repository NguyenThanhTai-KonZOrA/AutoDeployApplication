using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;

namespace ClientLauncher.Implement.Services
{
    public class VersionService : IVersionService
    {
        public LocalAppInfo GetLocalVersions(string appCode)
        {
            var basePath = $@"C:\CompanyApps\{appCode}";
            var binaryVersionFile = Path.Combine(basePath, "App", "version.txt");
            var configVersionFile = Path.Combine(basePath, "Config", "version.txt");

            return new LocalAppInfo
            {
                AppCode = appCode,
                BinaryVersion = File.Exists(binaryVersionFile) ? File.ReadAllText(binaryVersionFile).Trim() : "0.0.0",
                ConfigVersion = File.Exists(configVersionFile) ? File.ReadAllText(configVersionFile).Trim() : "0.0.0"
            };
        }

        public bool IsNewerVersion(string serverVersion, string localVersion)
        {
            try
            {
                var server = new Version(serverVersion);
                var local = new Version(localVersion);
                return server > local;
            }
            catch
            {
                return serverVersion != localVersion;
            }
        }

        public void SaveBinaryVersion(string appCode, string version)
        {
            var versionFile = $@"C:\CompanyApps\{appCode}\App\version.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(versionFile)!);
            File.WriteAllText(versionFile, version);
        }

        public void SaveConfigVersion(string appCode, string version)
        {
            var versionFile = $@"C:\CompanyApps\{appCode}\Config\version.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(versionFile)!);
            File.WriteAllText(versionFile, version);
        }
    }
}