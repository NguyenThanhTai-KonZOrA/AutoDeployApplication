using ClientLauncher.Implement.ViewModels;

namespace ClientLauncher.Implement.Services.Interface
{
    public interface IApplicationSettingsService
    {
        Task<List<ApplicationSettingDto>> GetAllSettingsAsync();
        Task<ApplicationSettingDto?> GetSettingByKeyAsync(string key);
        Task<List<ApplicationSettingDto>> GetSettingsByCategoryAsync(string category);
        Task<bool> AddSettingAsync(CreateSettingRequest request, string createdBy);
        Task<bool> UpdateSettingAsync(string key, string value, string updatedBy);
        Task<bool> BulkUpdateSettingsAsync(BulkUpdateSettingsRequest request, string updatedBy);
        Task<bool> DeleteSettingAsync(string key, string deletedBy);
        T? GetSettingValue<T>(string key);
        void ClearCache(string key);
        void ClearCacheBatch(IEnumerable<string> keys);
    }
}