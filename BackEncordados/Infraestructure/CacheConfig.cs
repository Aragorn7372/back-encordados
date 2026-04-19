using BackEncordados.Common.Service.Cache;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

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
        
        Log.Information("Configurando caché Redis...");
        var cacheUrl= Environment.GetEnvironmentVariable("REDIS_CACHE_URL") ?? configuration["redis:url"];
        if (string.IsNullOrEmpty(cacheUrl))
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "redis://red-d630v77gi27c7382gq10:6379";
                options.InstanceName = "Encordados:";
                services.TryAddScoped<ICacheService, CacheService>();
            });
        else
            services.AddMemoryCache();
        services.TryAddScoped<ICacheService, CacheService>();
        return services;
    }
}