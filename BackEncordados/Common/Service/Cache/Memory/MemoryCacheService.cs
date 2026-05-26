using Microsoft.Extensions.Caching.Memory;

namespace BackEncordados.Common.Service.Cache.Memory;

/// <summary>
/// Implementación de <see cref="ICacheService"/> que envuelve <see cref="IMemoryCache"/>
/// para proporcionar el nivel de caché L1 (memoria local del proceso).
/// </summary>
/// <remarks>
/// <para>Actúa como el nivel <b>L1</b> en la estrategia híbrida definida en
/// <see cref="Hybrid.HybridCacheService"/>. Proporciona acceso ultrarrápido (~0.1ms)
/// sin sobrecarga de red, pero los datos son volátiles (se pierden al reiniciar el proceso)
/// y no se comparten entre instancias.</para>
///
/// <para><b>Comportamiento destacado:</b></para>
/// <list type="bullet">
///   <item><description><b>GetAsync:</b> Utiliza <c>TryGetValue</c> sincrónico envuelto en
///   <see cref="Task.FromResult{TResult}"/>. No hay operaciones asincrónicas reales porque
///   la memoria local no requiere I/O.</description></item>
///   <item><description><b>SetAsync:</b> TTL por defecto de 5 minutos si no se especifica expiración.</description></item>
///   <item><description><b>RemoveByPatternAsync:</b> Operación no-op. <see cref="IMemoryCache"/>
///   no expone un mecanismo para enumerar o buscar claves por patrón.
///   La invalidación por patrón se delega a <see cref="Redis.RedisCacheService"/> (L2).</description></item>
/// </list>
///
/// <para>El TTL por defecto de 5 minutos balancea la frescura de los datos con la tasa de aciertos
/// en caché local. Este valor puede sobrescribirse por llamada mediante el parámetro
/// <c>expiration</c> de <see cref="SetAsync{T}"/>.</para>
/// </remarks>
/// <param name="cache">Instancia de <see cref="IMemoryCache"/> inyectada por el contenedor DI.</param>
public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    /// <summary>
    /// Obtiene un valor de la memoria caché local.
    /// </summary>
    /// <remarks>
    /// <para>Internamente utiliza <c>TryGetValue</c> de <see cref="IMemoryCache"/>.
    /// Al ser una operación puramente sincrónica (no hay E/S de red ni disco),
    /// se envuelve el resultado en <see cref="Task.FromResult{TResult}"/> para cumplir
    /// con el contrato asincrónico de <see cref="ICacheService"/>.</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor almacenado.</typeparam>
    /// <param name="key">Clave única del valor en caché.</param>
    /// <returns>El valor encontrado, o <c>default</c> si la clave no existe o expiró.</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        // Intentamos obtener el valor de forma sincrónica
        if (cache.TryGetValue(key, out T? value))
        {
            // Devolvemos una tarea ya completada con el resultado
            return await Task.FromResult(value);
        }
        
        return await Task.FromResult(default(T));
    }

    /// <summary>
    /// Almacena un valor en la memoria caché local.
    /// </summary>
    /// <remarks>
    /// <para>Si no se especifica expiración, se aplica un TTL predeterminado de 5 minutos.
    /// La operación es sincrónica y se envuelve en <see cref="Task.CompletedTask"/>
    /// para cumplir el contrato asincrónico.</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave única del valor.</param>
    /// <param name="value">Valor a almacenar.</param>
    /// <param name="expiration">Tiempo de expiración. Si es <c>null</c>, se usa <c>TimeSpan.FromMinutes(5)</c>.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        cache.Set(key, value, expiration ?? TimeSpan.FromMinutes(5));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Elimina una entrada de la memoria caché local.
    /// </summary>
    /// <remarks>
    /// <para>Operación sincrónica envuelta en <see cref="Task.CompletedTask"/>.
    /// Si la clave no existe, la operación es segura (no lanza excepción).</para>
    /// </remarks>
    /// <param name="key">Clave única del valor a eliminar.</param>
    public async Task RemoveAsync(string key) { cache.Remove(key); await Task.CompletedTask; }

    /// <summary>
    /// No-op intencional. La caché en memoria local no soporta eliminación por patrón.
    /// </summary>
    /// <remarks>
    /// <para><see cref="IMemoryCache"/> no expone un mecanismo para enumerar o buscar claves
    /// mediante patrones (ej: <c>"prefix:*"</c>). Esta funcionalidad solo está disponible
    /// en <see cref="Redis.RedisCacheService"/> (L2).</para>
    /// <para>El método existe para cumplir el contrato de <see cref="ICacheService"/>.
    /// La invalidación por patrón a nivel del sistema debe realizarse a través del
    /// <see cref="Hybrid.HybridCacheService"/> que delega esta operación exclusivamente a L2.</para>
    /// </remarks>
    /// <param name="pattern">Patrón ignorado por esta implementación.</param>
    public Task RemoveByPatternAsync(string pattern) => Task.CompletedTask; 
}