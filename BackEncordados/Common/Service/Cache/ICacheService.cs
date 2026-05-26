namespace BackEncordados.Common.Service.Cache;

/// <summary>
/// Define el contrato para los servicios de caché del sistema,
/// soportando una estrategia de dos niveles (L1 local + L2 distribuido).
/// </summary>
/// <remarks>
/// <para>El sistema de caché implementa una arquitectura híbrida con tres implementaciones:</para>
/// <list type="table">
///   <listheader>
///     <term>Nivel</term>
///     <description>Implementación</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term>L1</term>
///     <description><c>MemoryCacheService</c></description>
///     <description>Memoria local del proceso, acceso ~0.1ms, volátil por instancia</description>
///   </item>
///   <item>
///     <term>L2</term>
///     <description><c>CacheService</c> (Redis)</description>
///     <description>Caché distribuida compartida entre instancias, acceso ~1-5ms</description>
///   </item>
///   <item>
///     <term>Híbrido</term>
///     <description><c>HybridCacheService</c></description>
///     <description>Combina L1+L2 con read-through (<c>GetAsync</c>) y write-through (<c>SetAsync</c>)</description>
///   </item>
/// </list>
///
/// <para><b>Estrategia transversal:</b></para>
/// <list type="bullet">
///   <item><description><b>GetAsync:</b> Read-through. Consulta L1 primero, luego L2 con backfill a L1.</description></item>
///   <item><description><b>SetAsync:</b> Write-through. Escribe en ambos niveles simultáneamente. TTL default: 5 minutos.</description></item>
///   <item><description><b>RemoveAsync:</b> Elimina de ambos niveles. Seguro si la clave no existe.</description></item>
///   <item><description><b>RemoveByPatternAsync:</b> Elimina por patrón <b>solo en L2</b>. L1 (memoria) no expone sus claves para escaneo.</description></item>
/// </list>
///
/// <para>Todos los métodos de implementación (L2) envuelven operaciones en try/catch con logging
/// para garantizar tolerancia a fallos: si Redis no responde, se retorna <c>default</c> y se registra el error,
/// sin propagar la excepción al llamante.</para>
/// </remarks>
public interface ICacheService
{
    /// <summary>
    /// Obtiene un valor de la caché.
    /// </summary>
    /// <remarks>
    /// <para>Si la implementación subyacente (Redis) falla por timeout o desconexión,
    /// el método captura la excepción, registra el error y retorna <c>default</c>
    /// sin propagar la excepción al llamante (<b>fail-safe</b>).</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor almacenado en caché.</typeparam>
    /// <param name="key">Clave única del valor.</param>
    /// <returns>El valor deserializado encontrado, o <c>default</c> si no existe o hubo error.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Guarda un valor en la caché.
    /// </summary>
    /// <remarks>
    /// <para>Serializa el valor a JSON y lo almacena con expiración absoluta.
    /// Si no se especifica expiración, se aplica un TTL predeterminado de 5 minutos.</para>
    /// <para>Si Redis falla, se captura la excepción, se registra el error y la operación
    /// se completa silenciosamente (<b>fail-safe</b>).</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave única del valor.</param>
    /// <param name="value">Valor a almacenar.</param>
    /// <param name="expiration">Tiempo de expiración opcional (default: 5 minutos si es <c>null</c>).</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Elimina un valor de la caché por su clave.
    /// </summary>
    /// <remarks>
    /// <para>Si la clave no existe, la operación se completa sin errores.
    /// Si Redis falla, se captura la excepción, se registra y se completa silenciosamente (<b>fail-safe</b>).</para>
    /// </remarks>
    /// <param name="key">Clave única del valor a eliminar.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Elimina todas las entradas cuyas claves coincidan con un patrón.
    /// </summary>
    /// <remarks>
    /// <para>Esta operación solo está disponible en implementaciones que soporten
    /// búsqueda por patrón (Redis L2 mediante SCAN/DEL). En <c>MemoryCacheService</c> (L1)
    /// es un no-op porque <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// no expone sus claves internas.</para>
    /// <para>El patrón sigue la sintaxis Redis: <c>"users:*"</c>, <c>"torneos:activos:*"</c>.</para>
    /// </remarks>
    /// <param name="pattern">Patrón de búsqueda estilo Redis (ej: <c>"prefix:*"</c>).</param>
    Task RemoveByPatternAsync(string pattern);
}