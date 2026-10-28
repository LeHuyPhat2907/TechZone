using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TechZone.Application.Common.Interfaces;
using TechZone.Infrastructure.Configuration;

namespace TechZone.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly Lazy<IConnectionMultiplexer?> _lazyConnection;
    private readonly ConcurrentDictionary<string, (object Value, DateTime ExpiresAt)> _memoryFallback = new();
    private bool _hasLoggedConnectionWarning = false;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public RedisCacheService(IOptions<RedisSettings> settings, ILogger<RedisCacheService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _lazyConnection = new Lazy<IConnectionMultiplexer?>(() =>
        {
            if (!_settings.Enabled)
            {
                return null;
            }

            try
            {
                var config = ConfigurationOptions.Parse(_settings.ConnectionString);
                config.AbortOnConnectFail = false;
                config.ConnectTimeout = 2000;
                config.SyncTimeout = 2000;
                var muxer = ConnectionMultiplexer.Connect(config);
                return muxer;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not establish initial connection to Redis on {ConnectionString}. Operating in memory-fallback mode.", _settings.ConnectionString);
                return null;
            }
        });
    }

    private IDatabase? GetDatabase()
    {
        try
        {
            var muxer = _lazyConnection.Value;
            if (muxer != null && muxer.IsConnected)
            {
                return muxer.GetDatabase();
            }
        }
        catch (Exception ex)
        {
            if (!_hasLoggedConnectionWarning)
            {
                _logger.LogWarning(ex, "Redis connection failed. Falling back to local in-memory cache.");
                _hasLoggedConnectionWarning = true;
            }
        }
        return null;
    }

    private string GetFullKey(string key) => $"{_settings.InstanceName}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                var value = await db.StringGetAsync(fullKey);
                if (value.HasValue)
                {
                    return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
                }
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis StringGetAsync failed for key {Key}. Using memory fallback.", key);
            }
        }

        // Memory fallback
        if (_memoryFallback.TryGetValue(fullKey, out var cacheItem))
        {
            if (cacheItem.ExpiresAt >= DateTime.UtcNow)
            {
                if (cacheItem.Value is T directVal) return directVal;
                var json = JsonSerializer.Serialize(cacheItem.Value, JsonOptions);
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            _memoryFallback.TryRemove(fullKey, out _);
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        var fullKey = GetFullKey(key);
        var effectiveExpiry = expiry ?? TimeSpan.FromMinutes(30);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, JsonOptions);
                await db.StringSetAsync(fullKey, json, effectiveExpiry);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis StringSetAsync failed for key {Key}. Using memory fallback.", key);
            }
        }

        // Memory fallback
        _memoryFallback[fullKey] = (value, DateTime.UtcNow.Add(effectiveExpiry));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                await db.KeyDeleteAsync(fullKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis KeyDeleteAsync failed for key {Key}.", key);
            }
        }

        _memoryFallback.TryRemove(fullKey, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var fullPrefix = GetFullKey(prefix);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                var muxer = _lazyConnection.Value;
                if (muxer != null)
                {
                    foreach (var endpoint in muxer.GetEndPoints())
                    {
                        var server = muxer.GetServer(endpoint);
                        if (server.IsConnected)
                        {
                            var keys = server.Keys(pattern: $"{fullPrefix}*").ToArray();
                            if (keys.Length > 0)
                            {
                                await db.KeyDeleteAsync(keys);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis RemoveByPrefixAsync failed for prefix {Prefix}.", prefix);
            }
        }

        var matchingKeys = _memoryFallback.Keys.Where(k => k.StartsWith(fullPrefix)).ToList();
        foreach (var k in matchingKeys)
        {
            _memoryFallback.TryRemove(k, out _);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                return await db.KeyExistsAsync(fullKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis KeyExistsAsync failed for key {Key}.", key);
            }
        }

        if (_memoryFallback.TryGetValue(fullKey, out var cacheItem))
        {
            if (cacheItem.ExpiresAt >= DateTime.UtcNow) return true;
            _memoryFallback.TryRemove(fullKey, out _);
        }

        return false;
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                return await db.StringIncrementAsync(fullKey, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis StringIncrementAsync failed for key {Key}.", key);
            }
        }

        // Memory fallback
        lock (_memoryFallback)
        {
            if (_memoryFallback.TryGetValue(fullKey, out var cacheItem) && cacheItem.Value is long current)
            {
                var next = current + value;
                _memoryFallback[fullKey] = (next, cacheItem.ExpiresAt);
                return next;
            }
            else
            {
                _memoryFallback[fullKey] = (value, DateTime.UtcNow.AddHours(24));
                return value;
            }
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                return await db.StringDecrementAsync(fullKey, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis StringDecrementAsync failed for key {Key}.", key);
            }
        }

        // Memory fallback
        lock (_memoryFallback)
        {
            if (_memoryFallback.TryGetValue(fullKey, out var cacheItem) && cacheItem.Value is long current)
            {
                var next = current - value;
                _memoryFallback[fullKey] = (next, cacheItem.ExpiresAt);
                return next;
            }
            else
            {
                _memoryFallback[fullKey] = (-value, DateTime.UtcNow.AddHours(24));
                return -value;
            }
        }
    }
}
