using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TgBot_Proxy.Cache
{
    public class CachedProxyList
    {
        private static readonly TimeSpan FreshTtl = TimeSpan.FromHours(13);
        private static readonly TimeSpan StaleTtl = TimeSpan.FromHours(25);

        private readonly ILogger<CachedProxyList> _logger;
        private readonly IMemoryCache _memoryCache;

        public CachedProxyList(ILogger<CachedProxyList> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public void Cache<T>(List<T> model)
        {
            string cache_key = "HTTPCache_key" + typeof(T);
            string staleCachekey = "HTTPCache_key_stale" + typeof(T);
            try
            {
                if (model != null && model.Count != 0)
                {
                    var options = new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(TimeSpan.FromHours(12));

                    _memoryCache.Set(cache_key, model, options);

                    var staleoptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours(13))
                        .SetAbsoluteExpiration(TimeSpan.FromHours(25));

                    _memoryCache.Set(staleCachekey, model, staleoptions);

                    _logger.LogInformation($"✅ HTTP-кэш обновлён: {model.Count} прокси");
                }
                else
                {
                    _logger.LogWarning("Результат запроса HTTP пуст, кэш не обновлён");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
            }
        }

        public IReadOnlyList<T> GetCache<T>() where T : new()
        {
            string cache_key = "HTTPCache_key" + typeof(T);
            string staleCachekey = "HTTPCache_key_stale" + typeof(T);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out var cached) && cached is List<T> freshlist)
                {
                    return freshlist;
                }

                if (_memoryCache.TryGetValue(staleCachekey, out var stalecached) && stalecached is List<T> stalelist)
                {
                    return stalelist;
                }
                _logger.LogWarning("🆘 Нет ни fresh, ни stale HTTP данных");
                return new List<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                return new List<T>();
            }
        }
    }
}
