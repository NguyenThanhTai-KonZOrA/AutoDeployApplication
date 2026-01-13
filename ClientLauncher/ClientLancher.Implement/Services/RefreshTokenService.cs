using ClientLauncher.Implement.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ClientLauncher.Implement.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(IMemoryCache cache, ILogger<RefreshTokenService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public void StoreRefreshToken(string username, string refreshToken, DateTime expiration)
        {
            var cacheKey = $"refresh_token_{username}";
            _cache.Set(cacheKey, refreshToken, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            });
            _logger.LogInformation("Refresh token stored for user {Username}, expires at {Expiration}", username, expiration);
        }

        public bool ValidateRefreshToken(string username, string refreshToken)
        {
            var cacheKey = $"refresh_token_{username}";
            if (_cache.TryGetValue(cacheKey, out string? storedToken))
            {
                return storedToken == refreshToken;
            }
            return false;
        }

        public void RevokeRefreshToken(string username)
        {
            var cacheKey = $"refresh_token_{username}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Refresh token revoked for user {Username}", username);
        }
    }
}
