namespace ClientLauncher.Services.Interface
{
    public interface IVersionCheckService
    {
        Task<bool> IsUpdateAvailableAsync(string appCode);
        Task<string?> GetLatestVersionAsync(string appCode);
        Task<bool> IsForceUpdateRequiredAsync(string appCode);
    }
}