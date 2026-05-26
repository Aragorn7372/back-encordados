using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Repository;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Service;
using BackEncordados.Talleres.Dto;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Service.CrudService;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Service;

/// <summary>
/// Implementación de <see cref="IExcelService"/> que orquesta las operaciones
/// de exportación e importación Excel coordinando el repositorio de datos,
/// el administrador de archivos Excel y los servicios de negocio de cada módulo.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada (Facade pattern) centralizando la lógica de autorización,
/// transformación de datos y delegación a los servicios especializados.</para>
/// <para><b>Dependencias inyectadas:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>Tipo</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>excelRepository</c></term>
///     <term><see cref="IExcelRepository"/></term>
///     <description>Consultas de datos de torneos, usuarios, materiales y pedidos.</description>
///   </item>
///   <item>
///     <term><c>excelArchiveManager</c></term>
///     <term><see cref="IExcelArchiveManager"/></term>
///     <description>Creación y lectura de archivos Excel (formato ClosedXML).</description>
///   </item>
///   <item>
///     <term><c>talleresDbContext</c></term>
///     <term><see cref="TalleresDbContext"/></term>
///     <description>Consulta directa del nombre del torneo para títulos de archivo.</description>
///   </item>
///   <item>
///     <term><c>userService</c></term>
///     <term><see cref="IUserService"/></term>
///     <description>Operaciones CRUD de usuarios (crear y actualizar).</description>
///   </item>
///   <item>
///     <term><c>tournamentService</c></term>
///     <term><see cref="ITournamentService"/></term>
///     <description>Operaciones CRUD de torneos (crear y actualizar).</description>
///   </item>
///   <item>
///     <term><c>materialsService</c></term>
///     <term><see cref="IMaterialsService"/></term>
///     <description>Operaciones CRUD de materiales de encordado.</description>
///   </item>
///   <item>
///     <term><c>cuerdasService</c></term>
///     <term><see cref="ICuerdasService"/></term>
///     <description>Operaciones CRUD de cuerdas.</description>
///   </item>
///   <item>
///     <term><c>purchasedService</c></term>
///     <term><see cref="IPurchasedService"/></term>
///     <description>Operaciones CRUD de pedidos y líneas de pedido.</description>
///   </item>
///   <item>
///     <term><c>logger</c></term>
///     <term><c>ILogger&lt;ExcelService&gt;</c></term>
///     <description>Logging de operaciones y errores de importación.</description>
///   </item>
/// </list>
/// </remarks>
/// <param name="excelRepository">Repositorio de consultas de datos para Excel.</param>
/// <param name="excelArchiveManager">Administrador de archivos Excel (ClosedXML).</param>
/// <param name="talleresDbContext">DbContext de torneos para consultas directas.</param>
/// <param name="userService">Servicio CRUD de usuarios.</param>
/// <param name="tournamentService">Servicio CRUD de torneos.</param>
/// <param name="materialsService">Servicio CRUD de materiales.</param>
/// <param name="cuerdasService">Servicio CRUD de cuerdas.</param>
/// <param name="purchasedService">Servicio CRUD de pedidos.</param>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
public class ExcelService(
    IExcelRepository excelRepository,
    IExcelArchiveManager excelArchiveManager,
    TalleresDbContext talleresDbContext,
    IUserService userService,
    ITournamentService tournamentService,
    IMaterialsService materialsService,
    ICuerdasService cuerdasService,
    IPurchasedService purchasedService,
    ILogger<ExcelService> logger
) : IExcelService
{
    /// <summary>Lista blanca de tipos de datos válidos para exportación/importación.</summary>
    private static readonly List<string> ValidTypes = new() { "users", "materials", "cuerdas", "tournament", "pedidos" };

    /// <summary>
    /// Exporta un resumen simple del torneo en formato Excel.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica que el usuario sea supervisor del torneo mediante
    ///   <c>IExcelRepository.IsUserSupervisorOfTournamentAsync</c>.</description></item>
    ///   <item><description>Si no es supervisor → lanza <c>UnauthorizedAccessException</c>.</description></item>
    ///   <item><description>Obtiene los datos agregados del torneo (jugadores, raquetas, precios)
    ///   desde <c>IExcelRepository.GetTournamentDataAsync</c>.</description></item>
    ///   <item><description>Consulta el nombre del torneo desde <c>TalleresDbContext.Partidos</c>
    ///   para usarlo como título del archivo. Si el torneo no existe, usa
    ///   <c>$"Torneo {tournamentId}"</c> como fallback.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateExcelAsync</c> la generación
    ///   del archivo Excel a partir de los datos y el nombre del torneo.</description></item>
    /// </list>
    /// <para><b>Respuesta:</b> Retorna un arreglo de bytes listo para ser servido como
    /// <c>FileContentResult</c> con MIME type <c>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet</c>.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la exportación (ULID).</param>
    /// <param name="tournamentId">ID del torneo a exportar (ULID).</param>
    /// <returns>Arreglo de bytes con el archivo Excel generado.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no es supervisor del torneo.</exception>
    public async Task<byte[]> ExportTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var isSupervisor = await excelRepository.IsUserSupervisorOfTournamentAsync(userId, tournamentId);
        
        if (!isSupervisor)
        {
            throw new UnauthorizedAccessException("No tienes permisos para exportar este torneo");
        }

        var data = await excelRepository.GetTournamentDataAsync(tournamentId);

        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .Select(t => t.Title)
            .FirstOrDefaultAsync();

        var tournamentName = tournament ?? $"Torneo {tournamentId}";

        logger.LogInformation("Exporting tournament {TournamentId} - {TournamentName}", tournamentId, tournamentName);

        return await excelArchiveManager.CreateExcelAsync(data, tournamentName);
    }

    /// <summary>
    /// Exporta datos multi-hoja de un torneo según los tipos especificados.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica permisos según el rol del usuario:
    ///   ADMIN → acceso directo sin verificación adicional;
    ///   OWNER → verifica que sea propietario del torneo mediante
    ///   <c>IExcelRepository.IsUserOwnerOfTournamentAsync</c>.</description></item>
    ///   <item><description>Si no tiene acceso → lanza <c>UnauthorizedAccessException</c>.</description></item>
    ///   <item><description>Filtra los tipos solicitados contra la lista blanca <c>ValidTypes</c>
    ///   (conversión a minúsculas). Si <paramref name="types"/> está vacío,
    ///   se exportan todos los tipos disponibles.</description></item>
    ///   <item><description>Obtiene los datos multi-hoja desde el repositorio mediante
    ///   <c>IExcelRepository.GetAdvancedDataAsync</c>.</description></item>
    ///   <item><description>Consulta el nombre del torneo desde <c>TalleresDbContext</c> para el título.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateAdvancedExcelAsync</c> la generación
    ///   del archivo Excel multi-hoja.</description></item>
    /// </list>
    /// <para><b>Tipos válidos:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>"users"</c> — Usuarios del torneo.</description></item>
    ///   <item><description><c>"materials"</c> — Materiales de encordado.</description></item>
    ///   <item><description><c>"cuerdas"</c> — Cuerdas.</description></item>
    ///   <item><description><c>"tournament"</c> — Datos generales del torneo.</description></item>
    ///   <item><description><c>"pedidos"</c> — Pedidos y líneas de pedido.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la exportación (ULID).</param>
    /// <param name="tournamentId">ID del torneo a exportar (ULID).</param>
    /// <param name="types">Lista de tipos de datos a incluir. Vacío = todos.</param>
    /// <param name="role">Rol del usuario (ADMIN u OWNER).</param>
    /// <returns>Arreglo de bytes con el archivo Excel multi-hoja generado.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no tiene permisos para exportar este torneo.</exception>
    public async Task<byte[]> ExportAdvancedAsync(Ulid userId, Ulid tournamentId, List<string> types, string role)
    {
        var hasAccess = false;

        if (role == User.UserRoles.ADMIN)
        {
            hasAccess = true;
        }
        else if (role == User.UserRoles.OWNER)
        {
            hasAccess = await excelRepository.IsUserOwnerOfTournamentAsync(userId, tournamentId);
        }

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("No tienes permisos para exportar este torneo");
        }

        var validTypes = types.Count > 0 
            ? types.Where(t => ValidTypes.Contains(t.ToLower())).ToList() 
            : ValidTypes;

        var data = await excelRepository.GetAdvancedDataAsync(tournamentId, validTypes);

        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .Select(t => t.Title)
            .FirstOrDefaultAsync();

        var tournamentName = tournament ?? $"Torneo {tournamentId}";

        logger.LogInformation("Exporting advanced data for tournament {TournamentId} - {TournamentName}, types: {Types}", 
            tournamentId, tournamentName, string.Join(",", validTypes));

        return await excelArchiveManager.CreateAdvancedExcelAsync(data, validTypes, tournamentName);
    }

    /// <summary>
    /// Importa datos desde un archivo Excel a un torneo existente.
    /// </summary>
    /// <remarks>
    /// <para>Método principal de importación que orquesta la lectura del archivo Excel
    /// y la importación secuencial de cada módulo solicitado.</para>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Verifica permisos según el rol (ADMIN bypass, OWNER check).</description></item>
    ///   <item><description>Si no tiene acceso → lanza <c>UnauthorizedAccessException</c>.</description></item>
    ///   <item><description>Filtra los tipos solicitados contra la lista blanca <c>ValidTypes</c>.</description></item>
    ///   <item><description>Lee y parsea el archivo Excel mediante <c>IExcelArchiveManager.ReadExcelAsync</c>
    ///   que retorna un <see cref="ExcelAdvancedDataDto"/> con todos los datos del archivo.</description></item>
    ///   <item><description>Importa secuencialmente cada módulo según los tipos solicitados:
    ///   users → <c>ImportUsersAsync</c>, materials → <c>ImportMaterialsAsync</c>,
    ///   cuerdas → <c>ImportCuerdasAsync</c>, tournament → <c>ImportTournamentAsync</c>,
    ///   pedidos → <c>ImportPedidosAsync</c>.</description></item>
    ///   <item><description>Retorna un <see cref="ExcelImportResultDto"/> con contadores
    ///   de registros creados/actualizados y lista de errores.</description></item>
    /// </list>
    /// <para><b>Manejo de errores:</b> Cada fila se importa dentro de un try/catch individual.
    /// Los errores no detienen el proceso; se registran como warnings y se acumulan
    /// en <c>result.Errors</c> para revisión posterior.</para>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la importación (ULID).</param>
    /// <param name="tournamentId">ID del torneo destino (ULID).</param>
    /// <param name="types">Lista de tipos de datos a importar. Vacío = todos.</param>
    /// <param name="role">Rol del usuario (ADMIN u OWNER).</param>
    /// <param name="excelStream">Stream del archivo Excel (.xlsx) a importar.</param>
    /// <returns>Resultado de la importación con contadores y lista de errores.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no tiene permisos para importar a este torneo.</exception>
    public async Task<ExcelImportResultDto> ImportAsync(Ulid userId, Ulid tournamentId, List<string> types, string role, Stream excelStream)
    {
        var hasAccess = false;

        if (role == User.UserRoles.ADMIN)
        {
            hasAccess = true;
        }
        else if (role == User.UserRoles.OWNER)
        {
            hasAccess = await excelRepository.IsUserOwnerOfTournamentAsync(userId, tournamentId);
        }

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("No tienes permisos para importar a este torneo");
        }

        var validTypes = types.Count > 0 
            ? types.Where(t => ValidTypes.Contains(t.ToLower())).ToList() 
            : ValidTypes;

        logger.LogInformation("Starting import for tournament {TournamentId}, types: {Types}", tournamentId, string.Join(",", validTypes));

        var data = await excelArchiveManager.ReadExcelAsync(excelStream);
        var result = new ExcelImportResultDto();

        if (validTypes.Contains("users"))
        {
            await ImportUsersAsync(data.Users, tournamentId, result);
        }

        if (validTypes.Contains("materials"))
        {
            await ImportMaterialsAsync(data.Materials, tournamentId, result);
        }

        if (validTypes.Contains("cuerdas"))
        {
            await ImportCuerdasAsync(data.Cuerdas, tournamentId, result);
        }

        if (validTypes.Contains("tournament"))
        {
            await ImportTournamentAsync(data.Tournament, userId, result);
        }

        if (validTypes.Contains("pedidos"))
        {
            await ImportPedidosAsync(data.Pedidos, data.PedidoLineas, tournamentId, result);
        }

        logger.LogInformation("Import completed. Users: {Created} created, {Updated} updated. Materials: {MCreated} created, {MUpdated} updated. Pedidos: {PCreated} created, {PUpdated} updated",
            result.UsersCreated, result.UsersUpdated, result.MaterialsCreated, result.MaterialsUpdated, result.PedidosCreated, result.PedidosUpdated);

        return result;
    }

    /// <summary>
    /// Importa usuarios desde los datos Excel al torneo especificado.
    /// </summary>
    /// <remarks>
    /// <para><b>Decisión update vs create:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si <c>Id</c> no es nulo/vacío y es un ULID válido → actualiza usuario existente
    ///   mediante <c>IUserService.PatchUserAsync</c> con un <c>UserRequestDto</c>.</description></item>
    ///   <item><description>Si <c>Id</c> es nulo, vacío o ULID inválido → crea nuevo usuario como contacto
    ///   mediante <c>IUserService.CreateContacto</c> con un <c>ContactoPostRequestDto</c>.</description></item>
    /// </list>
    /// <para><b>Mapeo de propiedades:</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelUsersDto)</term><description>Update (UserRequestDto)</description><description>Create (ContactoPostRequestDto)</description></listheader>
    ///   <item><term>Username</term><description>Username</description><description>—</description></item>
    ///   <item><term>Name</term><description>Name</description><description>Name (fallback a Username o "Unknown")</description></item>
    ///   <item><term>Email</term><description>Email</description><description>Email</description></item>
    ///   <item><term>Phone</term><description>Telefono</description><description>Phone</description></item>
    ///   <item><term>TournamentId</term><description>—</description><description>TournamentId</description></item>
    /// </list>
    /// <para><b>Manejo de errores:</b> Cada usuario se procesa en un bloque try/catch individual.
    /// Los errores se registran como warning y se agregan a <c>result.Errors</c>.</para>
    /// </remarks>
    /// <param name="users">Lista de usuarios importados del Excel.</param>
    /// <param name="tournamentId">ID del torneo destino para nuevos contactos.</param>
    /// <param name="result">DTO de resultado donde se acumulan contadores y errores.</param>
    private async Task ImportUsersAsync(List<ExcelUsersDto> users, Ulid tournamentId, ExcelImportResultDto result)
    {
        foreach (var u in users)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(u.Id) && Ulid.TryParse(u.Id, out var userId))
                {
                    var request = new UserRequestDto
                    {
                        Username = u.Username,
                        Name = u.Name,
                        Email = u.Email,
                        Telefono = u.Phone
                    };
                    await userService.PatchUserAsync(userId, request);
                    result.UsersUpdated++;
                }
                else
                {
                    var request = new ContactoPostRequestDto
                    {
                        Name = u.Name ?? u.Username ?? "Unknown",
                        Email = u.Email,
                        Phone = u.Phone,
                        TournamentId = tournamentId
                    };
                    await userService.CreateContacto(request);
                    result.UsersCreated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error importing user {Username}", u.Username);
                result.Errors.Add($"Error importing user {u.Username}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Importa materiales desde los datos Excel al torneo especificado.
    /// </summary>
    /// <remarks>
    /// <para><b>Decisión update vs create:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si <c>Id &gt; 0</c> → actualiza material existente mediante
    ///   <c>IMaterialsService.UpdateAsync</c> con un <c>MaterialPatchDto</c>.</description></item>
    ///   <item><description>Si <c>Id == 0</c> → crea nuevo material mediante
    ///   <c>IMaterialsService.CreateAsync</c> con un <c>MaterialRequestDto</c>.</description></item>
    /// </list>
    /// <para><b>Mapeo de propiedades:</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelMaterialsDto)</term><description>Update (MaterialPatchDto)</description><description>Create (MaterialRequestDto)</description></listheader>
    ///   <item><term>Marca</term><description>Marca</description><description>Marca</description></item>
    ///   <item><term>Modelo</term><description>Modelo</description><description>Modelo</description></item>
    ///   <item><term>Stock</term><description>Stock</description><description>Stock</description></item>
    ///   <item><term>Precio</term><description>Precio</description><description>Precio</description></item>
    ///   <item><term>Type</term><description>Type</description><description>Type</description></item>
    ///   <item><term>TournamentId</term><description>—</description><description>TournamentId</description></item>
    /// </list>
    /// </remarks>
    /// <param name="materials">Lista de materiales importados del Excel.</param>
    /// <param name="tournamentId">ID del torneo destino para nuevos materiales.</param>
    /// <param name="result">DTO de resultado donde se acumulan contadores y errores.</param>
    private async Task ImportMaterialsAsync(List<ExcelMaterialsDto> materials, Ulid tournamentId, ExcelImportResultDto result)
    {
        foreach (var m in materials)
        {
            try
            {
                if (m.Id > 0)
                {
                    var patchDto = new MaterialPatchDto
                    {
                        Marca = m.Marca,
                        Modelo = m.Modelo,
                        Stock = m.Stock,
                        Precio = m.Precio,
                        Type = m.Type
                    };
                    await materialsService.UpdateAsync(m.Id, patchDto);
                    result.MaterialsUpdated++;
                }
                else
                {
                    var request = new MaterialRequestDto
                    {
                        Marca = m.Marca,
                        Modelo = m.Modelo,
                        Stock = m.Stock,
                        Precio = m.Precio,
                        Type = m.Type,
                        TournamentId = tournamentId
                    };
                    await materialsService.CreateAsync(request);
                    result.MaterialsCreated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error importing material {Marca} {Modelo}", m.Marca, m.Modelo);
                result.Errors.Add($"Error importing material {m.Marca} {m.Modelo}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Importa cuerdas desde los datos Excel al torneo especificado.
    /// </summary>
    /// <remarks>
    /// <para><b>Decisión update vs create:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si <c>Id &gt; 0</c> → actualiza cuerda existente mediante
    ///   <c>ICuerdasService.UpdateAsync</c> con un <c>CuerdaPatchDto</c>.</description></item>
    ///   <item><description>Si <c>Id == 0</c> → crea nueva cuerda mediante
    ///   <c>ICuerdasService.CreateAsync</c> con un <c>CuerdaRequestDto</c>.</description></item>
    /// </list>
    /// <para><b>Mapeo de propiedades:</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelCuerdasDto)</term><description>Update (CuerdaPatchDto)</description><description>Create (CuerdaRequestDto)</description></listheader>
    ///   <item><term>Marca</term><description>Marca</description><description>Marca</description></item>
    ///   <item><term>Modelo</term><description>Modelo</description><description>Modelo</description></item>
    ///   <item><term>Stock</term><description>Stock</description><description>Stock</description></item>
    ///   <item><term>Precio</term><description>Precio</description><description>Precio</description></item>
    ///   <item><term>Calibre</term><description>Calibre</description><description>Calibre</description></item>
    ///   <item><term>StringFormat</term><description>StringFormat</description><description>StringFormat</description></item>
    ///   <item><term>StringsType</term><description>StringsType</description><description>StringsType</description></item>
    ///   <item><term>TournamentId</term><description>—</description><description>TournamentId</description></item>
    /// </list>
    /// </remarks>
    /// <param name="cuerdas">Lista de cuerdas importadas del Excel.</param>
    /// <param name="tournamentId">ID del torneo destino para nuevas cuerdas.</param>
    /// <param name="result">DTO de resultado donde se acumulan contadores y errores.</param>
    private async Task ImportCuerdasAsync(List<ExcelCuerdasDto> cuerdas, Ulid tournamentId, ExcelImportResultDto result)
    {
        foreach (var c in cuerdas)
        {
            try
            {
                if (c.Id > 0)
                {
                    var patchDto = new CuerdaPatchDto
                    {
                        Marca = c.Marca,
                        Modelo = c.Modelo,
                        Stock = c.Stock,
                        Precio = c.Precio,
                        Calibre = c.Calibre,
                        StringFormat = c.StringFormat,
                        StringsType = c.StringsType
                    };
                    await cuerdasService.UpdateAsync(c.Id, patchDto);
                    result.CuerdasUpdated++;
                }
                else
                {
                    var request = new CuerdaRequestDto
                    {
                        Marca = c.Marca,
                        Modelo = c.Modelo,
                        Stock = c.Stock,
                        Precio = c.Precio,
                        Calibre = c.Calibre,
                        StringFormat = c.StringFormat,
                        StringsType = c.StringsType,
                        TournamentId = tournamentId
                    };
                    await cuerdasService.CreateAsync(request);
                    result.CuerdasCreated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error importing cuerda {Marca} {Modelo}", c.Marca, c.Modelo);
                result.Errors.Add($"Error importing cuerda {c.Marca} {c.Modelo}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Importa datos de torneos desde los datos Excel.
    /// </summary>
    /// <remarks>
    /// <para><b>Decisión update vs create:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si <c>Id</c> no es nulo/vacío y es un ULID válido → actualiza torneo existente
    ///   mediante <c>ITournamentService.UpdateTournament</c> con un <c>TournamentPatchDto</c>.</description></item>
    ///   <item><description>Si <c>Id</c> es nulo, vacío o ULID inválido → crea nuevo torneo
    ///   mediante <c>ITournamentService.OwnerCreateTournament</c> con un <c>TournamentRequestDto</c>.</description></item>
    /// </list>
    /// <para><b>Mapeo de propiedades:</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelTournamentDto)</term><description>Update (TournamentPatchDto)</description><description>Create (TournamentRequestDto)</description></listheader>
    ///   <item><term>Title</term><description>Name</description><description>Name</description></item>
    ///   <item><term>StartTournament</term><description>StartTournament</description><description>StartTournament</description></item>
    ///   <item><term>EndTournament</term><description>EndTournament</description><description>EndTournament</description></item>
    ///   <item><term>Owner/Logotype/Workers/Supervisors</term><description>— (no se actualizan)</description><description>— (no se envían en create)</description></item>
    /// </list>
    /// <para><b>Nota:</b> En la creación, el usuario autenticado (<paramref name="userId"/>) se asigna
    /// como propietario del torneo mediante el parámetro <c>userId</c> de <c>OwnerCreateTournament</c>.</para>
    /// </remarks>
    /// <param name="tournaments">Lista de torneos importados del Excel.</param>
    /// <param name="userId">ID del usuario autenticado (futuro Owner si se crea torneo nuevo).</param>
    /// <param name="result">DTO de resultado donde se acumulan contadores y errores.</param>
    private async Task ImportTournamentAsync(List<ExcelTournamentDto> tournaments, Ulid userId, ExcelImportResultDto result)
    {
        foreach (var t in tournaments)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(t.Id) && Ulid.TryParse(t.Id, out var tournamentUlid))
                {
                    var patchDto = new TournamentPatchDto
                    {
                        Name = t.Title,
                        StartTournament = t.StartTournament,
                        EndTournament = t.EndTournament
                    };
                    await tournamentService.UpdateTournament(tournamentUlid, patchDto);
                    result.TournamentsUpdated++;
                }
                else
                {
                    var request = new TournamentRequestDto
                    {
                        Name = t.Title,
                        StartTournament = t.StartTournament,
                        EndTournament = t.EndTournament
                    };
                    await tournamentService.OwnerCreateTournament(request, userId);
                    result.TournamentsCreated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error importing tournament {Title}", t.Title);
                result.Errors.Add($"Error importing tournament {t.Title}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Importa pedidos y sus líneas desde los datos Excel al torneo especificado.
    /// </summary>
    /// <remarks>
    /// <para>Método más complejo de importación. Procesa cada pedido y sus líneas asociadas.</para>
    /// <para><b>Decisión update vs create:</b></para>
    /// <list type="bullet">
    ///   <item><description>Si <c>Id</c> es un ULID válido Y existe en BD
    ///   (<c>purchasedService.FindByIdAsync</c> retorna <c>IsSuccess</c>)
    ///   → actualiza pedido + líneas existentes.</description></item>
    ///   <item><description>Si no → crea nuevo pedido con todas sus líneas anidadas.</description></item>
    /// </list>
    /// <para><b>Mapeo — Update de pedido (PurchasedPatchDto):</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelPedidosDto)</term><description>Propiedad DTO</description></listheader>
    ///   <item><term>Machine</term><description>Machine</description></item>
    ///   <item><term>Comments</term><description>Comments</description></item>
    ///   <item><term>PayStatus</term><description>PayStatus</description></item>
    /// </list>
    /// <para><b>Mapeo — Update de línea (PedidoLineaPatchDto):</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelPedidoLineasDto)</term><description>Propiedad DTO</description></listheader>
    ///   <item><term>RaquetModel</term><description>RaquetModel</description></item>
    ///   <item><term>Nudos</term><description>Nudos</description></item>
    ///   <item><term>DateString</term><description>DateString</description></item>
    ///   <item><term>Logotype</term><description>Logotype</description></item>
    ///   <item><term>Color</term><description>Color</description></item>
    ///   <item><term>Status</term><description>Status</description></item>
    ///   <item><term>StringV</term><description>StringSetup.StringV</description></item>
    ///   <item><term>TensionV</term><description>StringSetup.TensionV</description></item>
    ///   <item><term>PreStetchV</term><description>StringSetup.PreStetchV</description></item>
    ///   <item><term>StringH</term><description>StringSetup.StringH</description></item>
    ///   <item><term>TensionH</term><description>StringSetup.TensionH</description></item>
    ///   <item><term>PreStetchH</term><description>StringSetup.PreStetchH</description></item>
    /// </list>
    /// <para><b>Mapeo — Create de pedido (PurchasedRequestDto):</b></para>
    /// <list type="table">
    ///   <listheader><term>Excel (ExcelPedidosDto)</term><description>Propiedad DTO</description></listheader>
    ///   <item><term>PlayerId</term><description>PlayerName</description></item>
    ///   <item><term>AssignedTo</term><description>AssignedToName</description></item>
    ///   <item><term>Machine</term><description>Machine</description></item>
    ///   <item><term>Comments</term><description>Comments ("" si nulo)</description></item>
    ///   <item><term>Price</term><description>Price</description></item>
    ///   <item><term>PayStatus</term><description>PayStatus</description></item>
    ///   <item><term>TournamentId</term><description>TournamentId</description></item>
    ///   <item><term>Líneas</term><description>Lineas (lista de PedidoLineaRequestDto)</description></item>
    /// </list>
    /// <para><b>Notas técnicas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Las líneas se filtran por <c>PedidoId</c> para asociarlas al pedido correcto.</description></item>
    ///   <item><description>En update, primero se actualiza el pedido, luego cada línea individualmente.</description></item>
    ///   <item><description>En create, las líneas se envían anidadas dentro del DTO del pedido.</description></item>
    ///   <item><description><c>StringSetup</c> se mapea desde y hacia <c>StringSetupDto</c> con 6 propiedades.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="pedidos">Lista de pedidos importados del Excel.</param>
    /// <param name="lineas">Lista de líneas de pedido importadas del Excel.</param>
    /// <param name="tournamentId">ID del torneo destino para nuevos pedidos.</param>
    /// <param name="result">DTO de resultado donde se acumulan contadores y errores.</param>
    private async Task ImportPedidosAsync(List<ExcelPedidosDto> pedidos, List<ExcelPedidoLineasDto> lineas, Ulid tournamentId, ExcelImportResultDto result)
    {
        foreach (var p in pedidos)
        {
            try
            {
                var lineasDelPedido = lineas.Where(l => l.PedidoId == p.Id).ToList();

                if (!string.IsNullOrWhiteSpace(p.Id) && Ulid.TryParse(p.Id, out var pedidoId))
                {
                    var existingResult = await purchasedService.FindByIdAsync(pedidoId);
                    if (existingResult.IsSuccess)
                    {
                        var patchDto = new PurchasedPatchDto
                        {
                            Machine = p.Machine,
                            Comments = p.Comments,
                            PayStatus = p.PayStatus
                        };
                        await purchasedService.UpdatePurchasedAsync(pedidoId, patchDto);
                        result.PedidosUpdated++;

                        foreach (var l in lineasDelPedido)
                        {
                            if (!string.IsNullOrWhiteSpace(l.Id) && Ulid.TryParse(l.Id, out var lineaId))
                            {
                                var lineaPatchDto = new PedidoLineaPatchDto
                                {
                                    RaquetModel = l.RaquetModel,
                                    Nudos = l.Nudos,
                                    DateString = l.DateString,
                                    Logotype = l.Logotype,
                                    Color = l.Color,
                                    Status = l.Status,
                                    StringSetup = new StringSetupDto
                                    {
                                        StringV = l.StringV,
                                        TensionV = l.TensionV,
                                        PreStetchV = l.PreStetchV,
                                        StringH = l.StringH,
                                        TensionH = l.TensionH,
                                        PreStetchH = l.PreStetchH
                                    }
                                };
                                await purchasedService.UpdateLineaAsync(lineaId, lineaPatchDto);
                                result.PedidosLineasUpdated++;
                            }
                        }
                        continue;
                    }
                }

                var request = new PurchasedRequestDto
                {
                    TournamentId = tournamentId,
                    PlayerName = p.PlayerId,
                    AssignedToName = p.AssignedTo,
                    Machine = p.Machine,
                    Comments = p.Comments ?? "",
                    Price = p.Price,
                    PayStatus = p.PayStatus,
                    Lineas = lineasDelPedido.Select(l => new PedidoLineaRequestDto
                    {
                        RaquetModel = l.RaquetModel,
                        Nudos = l.Nudos,
                        DateString = l.DateString,
                        Logotype = l.Logotype,
                        Color = l.Color,
                        StringSetup = new StringSetupDto
                        {
                            StringV = l.StringV,
                            TensionV = l.TensionV,
                            PreStetchV = l.PreStetchV,
                            StringH = l.StringH,
                            TensionH = l.TensionH,
                            PreStetchH = l.PreStetchH
                        }
                    }).ToList()
                };

                await purchasedService.CreatePurchasedAsync(request);
                result.PedidosCreated++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error importing pedido {Id}", p.Id);
                result.Errors.Add($"Error importing pedido {p.Id}: {ex.Message}");
            }
        }
    }
}