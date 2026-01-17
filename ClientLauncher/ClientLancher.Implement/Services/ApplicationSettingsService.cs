using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ClientLauncher.Implement.Services
{
    public class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly IApplicationSettingsRepository _repository;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "AppSetting_";
        private const int CacheExpirationMinutes = 5;
        private readonly IUnitOfWork _unitOfWork;

        public ApplicationSettingsService(
            IApplicationSettingsRepository repository,
            IMemoryCache cache,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _cache = cache;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ApplicationSettingDto>> GetAllSettingsAsync()
        {
            var settings = await _repository.GetAllApplicationSettingsAsync();
            return settings.Select(MapToDto).ToList();
        }

        public async Task<ApplicationSettingDto?> GetSettingByKeyAsync(string key)
        {
            var setting = await GetCachedSettingAsync(key);
            return setting != null ? MapToDto(setting) : null;
        }

        public async Task<List<ApplicationSettingDto>> GetSettingsByCategoryAsync(string category)
        {
            var settings = await _repository.GetByCategoryAsync(category);
            return settings.Select(MapToDto).ToList();
        }

        public async Task<bool> UpdateSettingAsync(string key, string value, string updatedBy)
        {
            var result = await _repository.UpdateSettingAsync(key, value, updatedBy);
            if (result)
            {
                ClearCache(key);
            }
            return result;
        }

        public async Task<bool> BulkUpdateSettingsAsync(BulkUpdateSettingsRequest request, string updatedBy)
        {
            foreach (var setting in request.Settings)
            {
                await UpdateSettingAsync(setting.Key, setting.Value, updatedBy);
            }
            return true;
        }

        public T? GetSettingValue<T>(string key)
        {
            var cacheKey = $"{CacheKeyPrefix}{key}";

            if (!_cache.TryGetValue(cacheKey, out ApplicationSettings? setting))
            {
                setting = _repository.GetByKeyAsync(key).GetAwaiter().GetResult();

                if (setting != null)
                {
                    _cache.Set(cacheKey, setting, TimeSpan.FromMinutes(CacheExpirationMinutes));
                }
            }

            if (setting?.Value == null) return default;

            try
            {
                var targetType = typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)setting.Value;

                if (targetType == typeof(int) || targetType == typeof(int?))
                    return (T)(object)int.Parse(setting.Value);

                if (targetType == typeof(bool) || targetType == typeof(bool?))
                    return (T)(object)bool.Parse(setting.Value);

                if (targetType == typeof(double) || targetType == typeof(double?))
                    return (T)(object)double.Parse(setting.Value);

                // For complex objects stored as JSON
                return JsonSerializer.Deserialize<T>(setting.Value);
            }
            catch
            {
                return default;
            }
        }

        // Public method to clear specific cache
        public void ClearCache(string key)
        {
            var cacheKey = $"{CacheKeyPrefix}{key}";
            _cache.Remove(cacheKey);
        }

        // Clear cache for multiple keys
        public void ClearCacheBatch(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                ClearCache(key);
            }
        }

        // ApplicationSettingsService class

        public async Task<bool> AddSettingAsync(CreateSettingRequest request, string createdBy)
        {
            var setting = new ApplicationSettings
            {
                Key = request.Key,
                Value = request.Value,
                Description = request.Description,
                Category = request.Category,
                DataType = request.DataType,
                IsActive = true,
                IsDelete = false,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(setting);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSettingAsync(string key, string deletedBy)
        {
            var setting = await _repository.GetByKeyAsync(key);
            if (setting == null) return false;

            // Soft delete
            setting.IsDelete = true;
            setting.IsActive = false;
            setting.UpdatedBy = deletedBy;
            setting.UpdatedAt = DateTime.UtcNow;

            _repository.Update(setting);
            ClearCache(key);

            return true;
        }

        private async Task<ApplicationSettings?> GetCachedSettingAsync(string key)
        {
            var cacheKey = $"{CacheKeyPrefix}{key}";

            if (!_cache.TryGetValue(cacheKey, out ApplicationSettings? setting))
            {
                setting = await _repository.GetByKeyAsync(key);

                if (setting != null)
                {
                    _cache.Set(cacheKey, setting, TimeSpan.FromMinutes(CacheExpirationMinutes));
                }
            }

            return setting;
        }

        private ApplicationSettingDto MapToDto(ApplicationSettings entity)
        {
            return new ApplicationSettingDto
            {
                Id = entity.Id,
                Key = entity.Key,
                Value = entity.Value,
                Description = entity.Description,
                Category = entity.Category,
                DataType = entity.DataType,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy
            };
        }
    }
}