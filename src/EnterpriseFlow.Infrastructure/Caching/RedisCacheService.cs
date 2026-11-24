using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EnterpriseFlow.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public RedisCacheService(
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _redisConnection = redisConnection;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cache key '{Key}' from Redis. Falling back to source.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions();
            if (slidingExpiration.HasValue)
            {
                options.SetSlidingExpiration(slidingExpiration.Value);
            }
            else if (absoluteExpiration.HasValue)
            {
                options.SetAbsoluteExpiration(absoluteExpiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            }

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);
            _trackedKeys.TryAdd(key, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache key '{Key}' in Redis.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _trackedKeys.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key '{Key}' from Redis.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            // If native Redis connection is available, find and delete keys by pattern
            if (_redisConnection != null && _redisConnection.IsConnected)
            {
                var endpoints = _redisConnection.GetEndPoints();
                var server = _redisConnection.GetServer(endpoints[0]);
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();

                if (keys.Length > 0)
                {
                    var db = _redisConnection.GetDatabase();
                    await db.KeyDeleteAsync(keys);
                    _logger.LogInformation("Invalidated {Count} Redis keys matching prefix '{Prefix}'", keys.Length, prefix);
                }
            }

            // Also clean up from internal tracker
            var matchingKeys = _trackedKeys.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in matchingKeys)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
                _trackedKeys.TryRemove(key, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache keys for prefix '{Prefix}'.", prefix);
        }
    }
}
