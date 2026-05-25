using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Repository;

/// <summary>
/// Contrato del repositorio para la obtención de datos de torneos
/// con destino a exportación e importación en formato Excel.
/// </summary>
/// <remarks>
/// <para>Agrupa consultas a los cuatro DbContexts de la aplicación
/// (<c>PedidosDbContext</c>, <c>UserDbContext</c>, <c>TalleresDbContext</c>, <c>MaterialsDbContext</c>)
/// bajo una sola interfaz. Esto facilita el testeo unitario mediante mocking
/// y centraliza la lógica de acceso a datos para los servicios
/// <see cref="IExcelService"/>.</para>
/// <para><b>Métodos del contrato:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Retorno</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>GetTournamentDataAsync</c></term>
///     <description><c>IEnumerable&lt;TournamentExcelRowDto&gt;</c></description>
///     <description>Resumen simple del torneo: agrupa pedidos por jugador con conteo de raquetas y suma de precios.</description>
///   </item>
///   <item>
///     <term><c>IsUserSupervisorOfTournamentAsync</c></term>
///     <description><c>bool</c></description>
///     <description>Verifica si un usuario está en la lista de supervisores del torneo.</description>
///   </item>
///   <item>
///     <term><c>IsUserOwnerOfTournamentAsync</c></term>
///     <description><c>bool</c></description>
///     <description>Verifica si un usuario es el propietario del torneo.</description>
///   </item>
///   <item>
///     <term><c>GetAdvancedDataAsync</c></term>
///     <description><c>ExcelAdvancedDataDto</c></description>
///     <description>Exportación multi-hoja filtrada por lista de tipos de datos.</description>
///   </item>
/// </list>
/// <para>Todos los métodos lanzan <c>KeyNotFoundException</c> si el tournamentId no existe
/// (excepto <c>IsUserSupervisorOfTournamentAsync</c> y <c>IsUserOwnerOfTournamentAsync</c>
/// que retornan <c>false</c> ante torneo inexistente).</para>
/// </remarks>
public interface IExcelRepository
{
    /// <summary>
    /// Obtiene el resumen simple de un torneo agrupando pedidos por jugador.
    /// </summary>
    /// <remarks>
    /// <para>Cada fila del resultado (<see cref="TournamentExcelRowDto"/>) contiene
    /// el nombre de usuario, nombre completo, cantidad de raquetas encordadas
    /// y el precio total acumulado.</para>
    /// <para>Si no hay pedidos para el torneo, retorna una colección vacía.</para>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo a consultar (ULID).</param>
    /// <returns>Colección de filas con resumen por jugador, ordenadas por nombre de usuario.</returns>
    Task<IEnumerable<TournamentExcelRowDto>> GetTournamentDataAsync(Ulid tournamentId);

    /// <summary>
    /// Verifica si un usuario está en la lista de supervisores de un torneo.
    /// </summary>
    /// <remarks>
    /// <para>Busca el torneo por ID y luego verifica si <paramref name="userId"/>
    /// está presente en <c>SupervisorList</c> (búsqueda lineal O(n)).</para>
    /// <para>Si el torneo no existe, retorna <c>false</c> en lugar de lanzar excepción.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario a verificar (ULID).</param>
    /// <param name="tournamentId">ID del torneo (ULID).</param>
    /// <returns><c>true</c> si el usuario es supervisor del torneo; <c>false</c> en caso contrario.</returns>
    Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId);

    /// <summary>
    /// Verifica si un usuario es el propietario de un torneo.
    /// </summary>
    /// <remarks>
    /// <para>Compara <paramref name="userId"/> con <c>Owner</c> del torneo.</para>
    /// <para>Si el torneo no existe, retorna <c>false</c> en lugar de lanzar excepción.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario a verificar (ULID).</param>
    /// <param name="tournamentId">ID del torneo (ULID).</param>
    /// <returns><c>true</c> si el usuario es el propietario; <c>false</c> en caso contrario.</returns>
    Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId);

    /// <summary>
    /// Obtiene datos multi-hoja de un torneo para exportación avanzada.
    /// </summary>
    /// <remarks>
    /// <para>Consulta los módulos especificados en <paramref name="types"/> y
    /// popula únicamente las listas correspondientes en el DTO de retorno.
    /// Los módulos no solicitados quedan como listas vacías.</para>
    /// <para>Usa <c>AsNoTracking()</c> en todas las consultas por ser de solo lectura.</para>
    /// </remarks>
    /// <param name="tournamentId">ID del torneo (ULID).</param>
    /// <param name="types">Lista de tipos de datos a incluir. Valores válidos:
    /// "users", "materials", "cuerdas", "tournament", "pedidos".</param>
    /// <returns>DTO con los datos solicitados agrupados por módulo.</returns>
    Task<ExcelAdvancedDataDto> GetAdvancedDataAsync(Ulid tournamentId, List<string> types);
}