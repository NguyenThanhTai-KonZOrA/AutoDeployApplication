using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface ISelectiveUpdateService
    {
        Task<bool> ApplySelectiveConfigUpdateAsync(string appCode, ManifestDto manifest, string packagePath);
    }
}