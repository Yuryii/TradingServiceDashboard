using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Dashboard.Data;
using Dashboard.Models;
using Dashboard.Services.Interfaces;

namespace Dashboard.Services;

public class NotificationConfigCache : INotificationConfigCache
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKey = "notification_configs_all";
    private const string ConfigKeyPrefix = "notification_config_";

    public NotificationConfigCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<List<NotificationConfig>> GetAllConfigsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<NotificationConfig>? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configs = await context.NotificationConfigs.AsNoTracking().ToListAsync();
        _cache.Set(CacheKey, configs, CacheTtl);
        return configs;
    }

    public async Task<NotificationConfig?> GetConfigAsync(string notificationCode)
    {
        var cacheKey = ConfigKeyPrefix + notificationCode;
        if (_cache.TryGetValue(cacheKey, out NotificationConfig? cached))
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = await context.NotificationConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NotificationCode == notificationCode);

        if (config != null)
            _cache.Set(cacheKey, config, CacheTtl);

        return config;
    }

    public async Task InvalidateAsync()
    {
        _cache.Remove(CacheKey);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var codes = await context.NotificationConfigs
            .AsNoTracking()
            .Select(c => c.NotificationCode)
            .ToListAsync();

        foreach (var code in codes)
            _cache.Remove(ConfigKeyPrefix + code);
    }
}
