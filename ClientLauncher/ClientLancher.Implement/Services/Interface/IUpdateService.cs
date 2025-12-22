namespace ClientLancher.Implement.Services.Interface
{
    public interface IUpdateService
    {
        Task<bool> CheckAndApplyUpdatesAsync(string appCode);
    }
}