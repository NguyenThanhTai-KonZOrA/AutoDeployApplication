using ClientLancher.Implement.EntityModels;

namespace ClientLancher.Implement.Repositories.Interface
{
    public interface IApplicationRepository : IGenericRepository<Application>
    {
        Task<Application?> GetByAppCodeAsync(string appCode);
        Task<IEnumerable<Application>> GetActiveApplicationsAsync();
        Task<IEnumerable<Application>> GetApplicationsByCategoryAsync(string category);
        void Delete(Application application);
        Task<IEnumerable<Application>> GetActiveApplicationsForAdminAsync();
    }
}