using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.Hybrid;
using BackEncordados.Common.Service.Cache.Memory;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Extensiones de configuración de caché.
/// </summary>
public static class CacheConfig
{
    /// <summary>
    /// Configura el servicio de caché.
    /// Desarrollo: MemoryCache.
    /// Producción: Redis.
    /// </summary>
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        // infraestructura base
        services.AddMemoryCache();
    
        var cacheUrl = Environment.GetEnvironmentVariable("REDIS_CACHE_URL") ?? configuration["redis:url"];
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = cacheUrl ?? throw new InvalidOperationException("Redis cache URL no configurada");
            options.InstanceName = "Encordados:";
        });

        // implementaciones a lo koin con llaves para poder diferenciar
        services.AddKeyedScoped<ICacheService, MemoryCacheService>("L1");
        services.AddKeyedScoped<ICacheService, CacheService>("L2");

        //  servicio hibridop
        services.AddScoped<ICacheService, HybridCacheService>();

        return services;
    }
}