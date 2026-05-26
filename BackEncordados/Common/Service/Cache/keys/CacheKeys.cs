namespace BackEncordados.Common.Service.Cache.keys;

/// <summary>
/// Contenedor de constantes y claves prefijadas para la caché del sistema.
/// </summary>
/// <remarks>
/// <para>Todas las claves se construyen como prefijos con guion bajo final (<c>"_"</c>)
/// para permitir su uso con <see cref="ICacheService.RemoveByPatternAsync"/> combinando
/// el prefijo con un identificador (ej: <c>$"{CacheKeys.TournamentCacheKey}{id}"</c>).</para>
/// <para><b>Diferencia entre <c>const</c> y <c>static readonly</c>:</b></para>
/// <list type="bullet">
///   <item><description><c>const</c>: valor en tiempo de compilación, se inlining en el ensamblado llamante.</description></item>
///   <item><description><c>static readonly</c>: valor en tiempo de ejecución, evita problemas de versionado
///   si el valor cambia sin recompilar los consumidores.</description></item>
/// </list>
/// </remarks>
public static class CacheKeys
{
    /// <summary>Prefijo para claves de torneos en caché. Valor: <c>"tournaments_"</c>.</summary>
    public static readonly string TournamentCacheKey = "tournaments_";
    /// <summary>Prefijo para claves de búsqueda por nombre de usuario. Valor: <c>"user_name_"</c>.</summary>
    public const string UserKey = "user_name_"; 
    /// <summary>Prefijo para claves de datos completos de usuario. Valor: <c>"user_data_"</c>.</summary>
    public const string UserDataKey = "user_data_"; 
    /// <summary>Prefijo para claves de pedidos comprados en caché. Valor: <c>"purchased_"</c>.</summary>
    public static readonly string PurchasedCacheKey = "purchased_";
    /// <summary>Prefijo para claves de tokens/códigos de cambio de contraseña. Valor: <c>"password_"</c>.</summary>
    public const string PasswordChange = "password_";
}