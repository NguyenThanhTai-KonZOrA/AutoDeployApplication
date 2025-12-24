using ClientLancher.Implement.ViewModels.Request;

namespace ClientLancher.Implement.Services.Interface
{
    public interface IServerManifestService
    {
        Task<AppManifest?> GetManifestAsync(string appCode);
        Task UpdateManifestAsync(string appCode, AppManifest manifest);
    }
}