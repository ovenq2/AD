// Services/CacheService.cs
using Microsoft.Extensions.Caching.Memory;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // Cache key prefixes - BURADA TANIMLANMALI
        public static class CacheKeys
        {
            public const string UserDetails = "user_details_";
            public const string GroupDetails = "group_details_";
            public const string PendingRequests = "pending_requests";
            public const string UserGroups = "user_groups_";
            public const string UserSearch = "user_search_";
            public const string GroupSearch = "group_search_";
        }

        public T? Get<T>(string key)
        {
            try
            {
                _cache.TryGetValue(key, out T? value);
                if (value != null)
                {
                    _logger.LogDebug($"Cache HIT for key: {key}");
                }
                else
                {
                    _logger.LogDebug($"Cache MISS for key: {key}");
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting cache for key: {key}");
                return default;
            }
        }

        public Task<T?> GetAsync<T>(string key)
        {
            return Task.FromResult(Get<T>(key));
        }

        public void Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();

                if (expiry.HasValue)
                    options.SetAbsoluteExpiration(expiry.Value);
                else
                    options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5)); // Default 5 dakika

                _cache.Set(key, value, options);
                _logger.LogDebug($"Cache SET for key: {key}, Expiry: {expiry}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting cache for key: {key}");
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            Set(key, value, expiry);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug($"Cache REMOVED for key: {key}");
        }

        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public bool Exists(string key)
        {
            return _cache.TryGetValue(key, out _);
        }

        // Cache temizleme metodu
        public void ClearUserCache(string username)
        {
            Remove($"{CacheKeys.UserDetails}{username}");
            Remove($"{CacheKeys.UserGroups}{username}");
            _logger.LogInformation($"User cache cleared for: {username}");
        }

        // Tüm cache'i temizle
        public void ClearAll()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
                _logger.LogInformation("All cache cleared");
            }
        }
    }
}