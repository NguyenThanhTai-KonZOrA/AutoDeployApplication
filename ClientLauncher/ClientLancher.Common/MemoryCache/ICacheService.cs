namespace ClientLauncher.Common.MemoryCache
{
    public interface ICacheService
    {
        bool TryGetValue<T>(string key, out T value);
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);
        void Remove(string key);
    }
}
