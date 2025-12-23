using ClientLauncher.Models;

namespace ClientLauncher.Services
{
    public interface IVersionCheckService
    {
        Task<VersionComparisonResult> CheckForUpdatesAsync(string appCode);
    }
}