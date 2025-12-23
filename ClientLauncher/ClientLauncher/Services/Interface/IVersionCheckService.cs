using ClientLauncher.Models;

namespace ClientLauncher.Services.Interface
{
    public interface IVersionCheckService
    {
        Task<VersionComparisonResult> CheckForUpdatesAsync(string appCode);
    }
}