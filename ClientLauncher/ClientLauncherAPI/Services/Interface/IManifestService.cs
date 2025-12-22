using AppServer.API.Models;
using System.Text.Json;

namespace ClientLauncherAPI.Services.Interface
{
    public interface IManifestService
    {
        Task<AppManifest?> GetManifestAsync(string appCode);
        Task UpdateManifestAsync(string appCode, AppManifest manifest);
    }

    
}