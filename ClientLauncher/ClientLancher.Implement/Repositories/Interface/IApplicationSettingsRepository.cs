using ClientLauncher.Implement.EntityModels;

namespace ClientLauncher.Implement.Repositories.Interface
{
    public interface IApplicationSettingsRepository : IGenericRepository<ApplicationSettings>
    {
        Task<List<ApplicationSettings>> GetAllApplicationSettingsAsync();
        Task<ApplicationSettings?> GetByKeyAsync(string key);
        Task<List<ApplicationSettings>> GetByCategoryAsync(string category);
        Task<bool> UpdateSettingAsync(string key, string value, string updatedBy);
        Task AddAsync(ApplicationSettings setting);
        void Update(ApplicationSettings setting);
    }
}