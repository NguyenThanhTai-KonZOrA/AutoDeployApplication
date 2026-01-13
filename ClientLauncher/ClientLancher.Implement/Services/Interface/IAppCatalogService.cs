using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IAppCatalogService
    {
        Task<IEnumerable<Application>> GetAllApplicationsAsync();
        Task<IEnumerable<Application>> GetApplicationsByCategoryAsync(string category);
        Task<Application?> GetApplicationAsync(string appCode);
        Task<bool> IsApplicationInstalledAsync(string appCode);
        Task<string?> GetInstalledVersionAsync(string appCode);
    }
}