using AppServer.API.Models;
using System.IO.Compression;
using System.Text.Json;

namespace ClientLauncherAPI.Services.Interface
{
    public interface IUpdateService
    {
        Task<bool> CheckAndApplyUpdatesAsync(string appCode);
    }

    
}