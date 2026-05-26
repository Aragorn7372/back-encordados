namespace BackEncordados.Common.Service.Cache.Hybrid;

/// <summary>
/// Implementación de <see cref="ICacheService"/> que combina dos niveles de caché
/// en una estrategia híbrida de lectura y escritura simultánea.
/// </summary>
/// <remarks>
/// <para>Utiliza dos servicios de caché registrados con clave nombrada:</para>
/// <list type="bullet">
///   <item><description><b>L1 ("L1"):</b> Caché en memoria local (ej: <c>IMemoryCache</c>).
///   Proporciona acceso ultrarrápido (~0.1ms) pero es volátil y no compartida entre instancias.</description></item>
///   <item><description><b>L2 ("L2"):</b> Caché distribuida (ej: Redis).
///   Proporciona acceso rápido (~1-5ms) y está compartida entre todas las instancias de la aplicación.</description></item>
/// </list>
///
/// <para><b>Estrategia de operaciones:</b></para>
/// <list type="bullet">
///   <item><description><b>GetAsync:</b> Read-through. Consulta L1 primero; si no encuentra, consulta L2 y
///   pobla L1 con un TTL corto (1 minuto) para evitar saturación.</description></item>
///   <item><description><b>SetAsync:</b> Write-through. Escribe en L1 y L2 simultáneamente.</description></item>
///   <item><description><b>RemoveAsync:</b> Elimina de L1 y L2 simultáneamente.</description></item>
///   <item><description><b>RemoveByPatternAsync:</b> Elimina solo de L2 (Redis soporta búsqueda por patrón;
///   la memoria caché local no).</description></item>
/// </list>
///
/// <para><b>Beneficio:</b> Reduce la carga sobre Redis al mantener los datos más consultados
/// en memoria local, disminuyendo la latencia promedio de lectura y el ancho de banda de red.</para>
/// </remarks>
/// <param name="l1">Servicio de caché de nivel 1 (memoria local). Registrado con <c>[FromKeyedServices("L1")]</c>.</param>
/// <param name="l2">Servicio de caché de nivel 2 (distribuido/Redis). Registrado con <c>[FromKeyedServices("L2")]</c>.</param>
public class HybridCacheService(
    [FromKeyedServices("L1")] ICacheService l1, 
    [FromKeyedServices("L2")] ICacheService l2) : ICacheService
{
    /// <summary>
    /// Obtiene un valor de la caché siguiendo la estrategia read-through de dos niveles.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo de ejecución:</b></para>
    /// <list type="number">
    ///   <item><description>Consulta L1 (memoria local). Si hay hit → devuelve el valor inmediatamente (~0.1ms).</description></item>
    ///   <item><description>Si miss en L1 → consulta L2 (Redis). Si hay hit → pobla L1 con TTL de 1 minuto y devuelve el valor.</description></item>
    ///   <item><description>Si miss en ambos → devuelve <c>default</c>.</description></item>
    /// </list>
    /// <para>El TTL corto en L1 (1 minuto) evita que datos poco consultados ocupen memoria local
    /// y asegura que los datos eventualmente consistentes se refresquen desde L2.</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor almacenado en caché.</typeparam>
    /// <param name="key">Clave única del valor en la caché.</param>
    /// <returns>El valor encontrado, o <c>default</c> si no existe en ningún nivel.</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        // Intentar RAM local
        var value = await l1.GetAsync<T>(key);
        if (value != null) return value;

        // Intentar Redis
        value = await l2.GetAsync<T>(key);
        if (value != null)
        {
            //  tiempo corto para no saturar
            await l1.SetAsync(key, value, TimeSpan.FromMinutes(1));
        }
        return value;
    }

    /// <summary>
    /// Guarda un valor en ambos niveles de caché simultáneamente (write-through).
    /// </summary>
    /// <remarks>
    /// <para>La escritura en L1 asegura que la siguiente lectura del mismo proceso sea rápida.
    /// La escritura en L2 asegura que otras instancias de la aplicación puedan acceder al dato.</para>
    /// <para>Ambas operaciones se ejecutan secuencialmente: primero L1, luego L2.
    /// Si L2 falla, el valor queda al menos en L1 para acceso local.</para>
    /// </remarks>
    /// <typeparam name="T">Tipo del valor a almacenar.</typeparam>
    /// <param name="key">Clave única del valor.</param>
    /// <param name="value">Valor a almacenar en caché.</param>
    /// <param name="expiration">Tiempo de expiración opcional. Si no se especifica, usa el valor por defecto de cada implementación.</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        await l1.SetAsync(key, value, expiration);
        await l2.SetAsync(key, value, expiration);
    }

    /// <summary>
    /// Elimina un valor de ambos niveles de caché simultáneamente.
    /// </summary>
    /// <remarks>
    /// <para>La eliminación en ambos niveles asegura que ninguna instancia de la aplicación
    /// pueda servir un dato obsoleto después de una operación de actualización o eliminación.</para>
    /// <para>Se ejecuta primero en L1, luego en L2. Si L2 falla, al menos se limpió la memoria local.</para>
    /// </remarks>
    /// <param name="key">Clave única del valor a eliminar.</param>
    public async Task RemoveAsync(string key)
    {
        await l1.RemoveAsync(key);
        await l2.RemoveAsync(key);
    }
    
    /// <summary>
    /// Elimina valores por patrón de clave solo en el nivel L2 (Redis).
    /// </summary>
    /// <remarks>
    /// <para>La caché L1 (memoria local) no soporta búsqueda por patrón, por lo que
    /// esta operación solo se aplica sobre L2. Es responsabilidad del llamante
    /// invalidar entradas específicas de L1 si es necesario usando <see cref="RemoveAsync"/>.</para>
    /// <para>El patrón sigue la sintaxis de Redis (ej: <c>"users:*"</c>, <c>"pedidos:t1:*"</c>).</para>
    /// </remarks>
    /// <param name="pattern">Patrón de búsqueda estilo Redis (ej: <c>"prefix:*"</c>).</param>
    public async Task RemoveByPatternAsync(string pattern) => await l2.RemoveByPatternAsync(pattern);
}