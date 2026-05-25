using BackEncordados.Excel.Dto;

namespace BackEncordados.Excel.Service;

/// <summary>
/// Contrato del servicio de lógica de negocio para exportación e importación
/// de datos de torneos en formato Excel.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada (Facade pattern) que orquesta las operaciones entre
/// el repositorio de datos (<see cref="IExcelRepository"/>), el administrador
/// de archivos Excel (<see cref="IExcelArchiveManager"/>), y los servicios
/// de negocio de cada módulo (usuarios, materiales, cuerdas, torneos, pedidos).</para>
/// <para><b>Métodos del contrato:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Método</term>
///     <description>Retorno</description>
///     <description>Rol requerido</description>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>ExportTournamentAsync</c></term>
///     <description><c>byte[]</c></description>
///     <description>Supervisor</description>
///     <description>Exporta resumen simple del torneo (jugadores, raquetas, precios).</description>
///   </item>
///   <item>
///     <term><c>ExportAdvancedAsync</c></term>
///     <description><c>byte[]</c></description>
///     <description>Owner/Admin</description>
///     <description>Exporta datos multi-hoja según tipos seleccionados.</description>
///   </item>
///   <item>
///     <term><c>ImportAsync</c></term>
///     <description><c>ExcelImportResultDto</c></description>
///     <description>Owner/Admin</description>
///     <description>Importa datos desde un archivo Excel subido.</description>
///   </item>
/// </list>
/// <para>Los métodos de exportación retornan el archivo Excel como arreglo de bytes
/// (<c>byte[]</c>) listo para ser servido como <c>FileContentResult</c> en el controlador.
/// El método de importación retorna un DTO con contadores de registros creados/actualizados
/// y una lista de errores ocurridos durante el proceso.</para>
/// <para>Todos los métodos lanzan <c>UnauthorizedAccessException</c> si el usuario
/// no tiene permisos para la operación solicitada.</para>
/// </remarks>
public interface IExcelService
{
    /// <summary>
    /// Exporta un resumen simple del torneo en formato Excel.
    /// </summary>
    /// <remarks>
    /// <para>Verifica que el usuario sea supervisor del torneo antes de exportar.
    /// El resumen incluye una fila por jugador con la cantidad de raquetas
    /// encordadas y el precio total acumulado.</para>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica permisos de supervisor mediante <c>IExcelRepository.IsUserSupervisorOfTournamentAsync</c>.</description></item>
    ///   <item><description>Obtiene los datos agregados del torneo desde el repositorio.</description></item>
    ///   <item><description>Obtiene el nombre del torneo desde <c>TalleresDbContext</c> para el título del archivo.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateExcelAsync</c> la generación del archivo Excel.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la exportación (ULID).</param>
    /// <param name="tournamentId">ID del torneo a exportar (ULID).</param>
    /// <returns>Arreglo de bytes con el archivo Excel generado.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no es supervisor del torneo.</exception>
    Task<byte[]> ExportTournamentAsync(Ulid userId, Ulid tournamentId);

    /// <summary>
    /// Exporta datos multi-hoja de un torneo según los tipos especificados.
    /// </summary>
    /// <remarks>
    /// <para>Los administradores (ADMIN) tienen acceso directo sin verificación adicional.
    /// Los propietarios (OWNER) deben ser verificados como dueños del torneo.</para>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica permisos según el rol (ADMIN bypass, OWNER check contra repositorio).</description></item>
    ///   <item><description>Filtra los tipos solicitados contra la lista blanca de tipos válidos
    ///   (<c>"users"</c>, <c>"materials"</c>, <c>"cuerdas"</c>, <c>"tournament"</c>, <c>"pedidos"</c>).</description></item>
    ///   <item><description>Si <paramref name="types"/> está vacío, se exportan todos los tipos disponibles.</description></item>
    ///   <item><description>Obtiene los datos multi-hoja desde el repositorio.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateAdvancedExcelAsync</c> la generación del Excel.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la exportación (ULID).</param>
    /// <param name="tournamentId">ID del torneo a exportar (ULID).</param>
    /// <param name="types">Lista de tipos de datos a incluir.
    /// Valores válidos: "users", "materials", "cuerdas", "tournament", "pedidos".</param>
    /// <param name="role">Rol del usuario (ADMIN u OWNER).</param>
    /// <returns>Arreglo de bytes con el archivo Excel multi-hoja generado.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no tiene permisos para exportar este torneo.</exception>
    Task<byte[]> ExportAdvancedAsync(Ulid userId, Ulid tournamentId, List<string> types, string role);

    /// <summary>
    /// Importa datos desde un archivo Excel a un torneo existente.
    /// </summary>
    /// <remarks>
    /// <para>Lee el archivo Excel desde el stream, parsea los datos por módulo,
    /// y para cada registro decide si actualizar (si existe ID) o crear (si no existe).
    /// Los errores individuales por fila no detienen el proceso; se acumulan en
    /// <c>ExcelImportResultDto.Errors</c>.</para>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica permisos según el rol (ADMIN bypass, OWNER check contra repositorio).</description></item>
    ///   <item><description>Filtra los tipos solicitados contra la lista blanca de tipos válidos.</description></item>
    ///   <item><description>Lee y parsea el archivo Excel mediante <c>IExcelArchiveManager.ReadExcelAsync</c>.</description></item>
    ///   <item><description>Importa secuencialmente cada módulo: users, materials, cuerdas, tournament, pedidos.</description></item>
    ///   <item><description>Retorna un DTO con contadores de registros creados/actualizados y errores.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la importación (ULID).</param>
    /// <param name="tournamentId">ID del torneo destino (ULID).</param>
    /// <param name="types">Lista de tipos de datos a importar.
    /// Valores válidos: "users", "materials", "cuerdas", "tournament", "pedidos".</param>
    /// <param name="role">Rol del usuario (ADMIN u OWNER).</param>
    /// <param name="excelStream">Stream del archivo Excel (.xlsx) a importar.</param>
    /// <returns>Resultado de la importación con contadores y lista de errores.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no tiene permisos para importar a este torneo.</exception>
    Task<ExcelImportResultDto> ImportAsync(Ulid userId, Ulid tournamentId, List<string> types, string role, Stream excelStream);
}