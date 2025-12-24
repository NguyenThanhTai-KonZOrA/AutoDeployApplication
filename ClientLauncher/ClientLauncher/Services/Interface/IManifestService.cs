using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IManifestService
    {
        Task<ManifestDto?> GetLocalManifestAsync(string appCode);
        Task<ManifestDto?> DownloadManifestFromServerAsync(string appCode);
        Task SaveManifestAsync(string appCode, ManifestDto manifest);
        string GetPackageName(ManifestDto manifest);
    }
}
