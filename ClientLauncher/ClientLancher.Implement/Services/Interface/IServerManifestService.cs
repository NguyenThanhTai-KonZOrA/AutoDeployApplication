using ClientLauncher.Implement.ViewModels.Request;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IServerManifestService
    {
        Task<AppManifest?> GetManifestAsync(string appCode);
        Task UpdateManifestAsync(string appCode, AppManifest manifest);
    }
}