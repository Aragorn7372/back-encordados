using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BackEncordados.Common.Service.Cache;

/// <summary>
/// Implementación de <see cref="ICacheService"/> basada en <see cref="IDistributedCache"/>
/// que actúa como el nivel L2 (Redis) del sistema de caché híbrido.
/// </summary>
/// <remarks>
/// <para>Esta clase es la implementación <b>L2</b> (distribuida/Redis) del contrato
/// <see cref="ICacheService"/>. Se inyecta con clave <c>"L2"</c> en el contenedor DI
/// para ser consumida por <see cref="Hybrid.HybridCacheService"/>.</para>
///
/// <para><b>Características:</b></para>
/// <list type="bullet">
///   <item><description><b>Serialización:</b> JSON con <c>PropertyNameCaseInsensitive = true</c>
///   para tolerancia a diferencias de mayúsculas/minúsculas entre orígenes de datos.</description></item>
///   <item><description><b>Tolerancia a fallos:</b> Todos los métodos envuelven operaciones Redis
///   en try/catch con logging. Si Redis falla, no se propaga la excepción al llamante.</description></item>
///   <item><description><b>TTL por defecto:</b> 5 minutos vía <see cref="DistributedCacheEntryOptions"/>
///   con <c>AbsoluteExpirationRelativeToNow</c>.</description></item>
///   <item><description><b>RemoveByPatternAsync:</b> No-op. <see cref="IDistributedCache"/> no expone
///   comandos SCAN/DEL por patrón; esta funcionalidad requiere acceso directo al <c>ConnectionMultiplexer</c>
///   de Redis (ver <see cref="Redis.RedisCacheService"/>).</description></item>
/// </list>
///
/// <para><b>Patrón implementado:</b> Cache-aside con escritura directa.
/// El llamante es responsable de poblar e invalidar la caché explícitamente.</para>
/// </remarks>
/// <param name="cache">Instancia de <see cref="IDistributedCache"/> (Redis StackExchange) inyectada por DI.</param>
/// <param name="logger">Logger para seguimiento de operaciones y errores.</param>
public class CacheService(
    IDistributedCache cache,
    ILogger<CacheService> logger
) : ICacheService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Obtiene un valor desde Redis, deserializando el JSON almacenado.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Llama a <c>GetStringAsync</c> de <see cref="IDistributedCache"/>.</description></item>
    ///   <item><description>Si el valor es <c>null</c> o vacío → log de miss, retorna <c>default</c>.</description></item>
    ///   <item><description>Si hay valor → log de hit, deserializa con <see cref="JsonSerializer"/> y retorna.</description></item>
    ///   <item><description>Si Redis lanza excepción (timeout, conexión) → log de error, retorna <c>default</c> (fail-safe).</description></item>
    /// </list>
    /// <para>Esta estrategia garantiza que la aplicación funcione aunque Redis no esté disponible,
    /// degradando a consultas directas a base de datos.</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor esperado.</typeparam>
    /// <param name="key">Clave única en Redis.</param>
    /// <returns>Valor deserializado, o <c>default</c> si no existe o hubo error.</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedValue))
            {
                logger.LogDebug("Cache miss para clave: {Key}", key);
                return default;
            }

            logger.LogDebug("Cache hit para clave: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener valor de caché para clave: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Guarda un valor en Redis serializado como JSON con expiración absoluta.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Serializa el valor a JSON con las opciones configuradas.</description></item>
    ///   <item><description>Crea <see cref="DistributedCacheEntryOptions"/> con <c>AbsoluteExpirationRelativeToNow</c>
    ///   igual al parámetro <c>expiration</c> o 5 minutos por defecto.</description></item>
    ///   <item><description>Llama a <c>SetStringAsync</c> de <see cref="IDistributedCache"/>.</description></item>
    ///   <item><description>Si Redis falla → log de error, operación silenciosa.</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave única del valor.</param>
    /// <param name="value">Valor a serializar y almacenar.</param>
    /// <param name="expiration">Tiempo de expiración (default: 5 minutos si es <c>null</c>).</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            await cache.SetStringAsync(key, jsonValue, options);

            logger.LogDebug("Valor cacheado para clave: {Key} con expiración: {Expiration}",
                key, expiration ?? TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al guardar en caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// Elimina una entrada de Redis por su clave.
    /// </summary>
    /// <remarks>
    /// <para>Si la clave no existe en Redis, la operación se completa sin errores.
    /// Si Redis falla → log de error, operación silenciosa.</para>
    /// </remarks>
    /// <param name="key">Clave única del valor a eliminar.</param>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
            logger.LogDebug("Entrada de caché eliminada para clave: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al eliminar de caché para clave: {Key}", key);
        }
    }

    /// <summary>
    /// No-op. <see cref="IDistributedCache"/> no expone operaciones de eliminación por patrón.
    /// </summary>
    /// <remarks>
    /// <para><b>Limitación conocida:</b> La abstracción <see cref="IDistributedCache"/>
    /// solo proporciona operaciones clave-valor individuales (Get, Set, Remove).
    /// No incluye comandos Redis como SCAN o KEYS para buscar por patrón.</para>
    /// <para>Para eliminación por patrón, utilizar <see cref="Redis.RedisCacheService"/>
    /// que opera directamente sobre <c>ConnectionMultiplexer</c> con comandos SCAN + DEL.</para>
    /// <para>Este método existe para cumplir el contrato de <see cref="ICacheService"/>.
    /// Solo registra en log la operación solicitada sin ejecutarla realmente.</para>
    /// </remarks>
    /// <param name="pattern">Patrón Redis ignorado por esta implementación.</param>
    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            logger.LogDebug("Eliminando entradas de caché que coinciden con patrón: {Pattern}", pattern);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al eliminar entradas de caché por patrón: {Pattern}", pattern);
        }
    }
}