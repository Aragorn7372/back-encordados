using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Dto;
using BackEncordados.Excel.Repository;
using BackEncordados.Materials.Dto.Materials;
using BackEncordados.Materials.Dto.Strings;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
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
/// de exportación e importación Excel coordinando los repositorios de datos
/// por DbContext, el administrador de archivos Excel y los servicios de negocio.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada (Facade pattern) centralizando la lógica de autorización,
/// transformación de datos y delegación a servicios y repositorios especializados.</para>
/// <para><b>Repositorios inyectados (uno por DbContext):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Repositorio</term>
///     <term>DbContext</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>excelUserRepository</c></term>
///     <term><see cref="UserDbContext"/></term>
///     <description>Consultas de usuarios.</description>
///   </item>
///   <item>
///     <term><c>excelPedidosRepository</c></term>
///     <term><see cref="PedidosDbContext"/></term>
///     <description>Consultas de pedidos y líneas.</description>
///   </item>
///   <item>
///     <term><c>excelTalleresRepository</c></term>
///     <term><see cref="TalleresDbContext"/></term>
///     <description>Consultas de torneos (supervisores, propietarios).</description>
///   </item>
///   <item>
///     <term><c>excelMaterialsRepository</c></term>
///     <term><see cref="MaterialsDbContext"/></term>
///     <description>Consultas de materiales y cuerdas.</description>
///   </item>
/// </list>
/// </remarks>
/// <param name="excelUserRepository">Repositorio de usuarios.</param>
/// <param name="excelPedidosRepository">Repositorio de pedidos.</param>
/// <param name="excelTalleresRepository">Repositorio de torneos.</param>
/// <param name="excelMaterialsRepository">Repositorio de materiales y cuerdas.</param>
/// <param name="excelArchiveManager">Administrador de archivos Excel (ClosedXML).</param>
/// <param name="talleresDbContext">DbContext de torneos para consultas directas.</param>
/// <param name="userService">Servicio CRUD de usuarios.</param>
/// <param name="tournamentService">Servicio CRUD de torneos.</param>
/// <param name="materialsService">Servicio CRUD de materiales.</param>
/// <param name="cuerdasService">Servicio CRUD de cuerdas.</param>
/// <param name="purchasedService">Servicio CRUD de pedidos.</param>
/// <param name="logger">Logger para seguimiento de operaciones.</param>
public class ExcelService(
    IExcelUserRepository excelUserRepository,
    IExcelPedidosRepository excelPedidosRepository,
    IExcelTalleresRepository excelTalleresRepository,
    IExcelMaterialsRepository excelMaterialsRepository,
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
    ///   <item><description>Verifica que el usuario sea supervisor del torneo.</description></item>
    ///   <item><description>Obtiene los pedidos del torneo desde <c>ExcelPedidosRepository</c>.</description></item>
    ///   <item><description>Obtiene los datos de usuarios desde <c>ExcelUserRepository</c>.</description></item>
    ///   <item><description>Agrupa pedidos por jugador y compone el resumen en memoria.</description></item>
    ///   <item><description>Consulta el nombre del torneo desde <c>TalleresDbContext</c> para el título.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateExcelAsync</c> la generación del Excel.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">ID del usuario que solicita la exportación (ULID).</param>
    /// <param name="tournamentId">ID del torneo a exportar (ULID).</param>
    /// <returns>Arreglo de bytes con el archivo Excel generado.</returns>
    /// <exception cref="UnauthorizedAccessException">El usuario no es supervisor del torneo.</exception>
    public async Task<byte[]> ExportTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var isSupervisor = await excelTalleresRepository.IsUserSupervisorOfTournamentAsync(userId, tournamentId);

        if (!isSupervisor)
        {
            throw new UnauthorizedAccessException("No tienes permisos para exportar este torneo");
        }

        var pedidos = await excelPedidosRepository.GetPedidosByTournamentAsync(tournamentId);
        var playerIds = pedidos.Select(p => p.PlayerId).Distinct().ToList();
        var usersDict = await excelUserRepository.GetUsersDictByIdsAsync(playerIds);

        var data = pedidos
            .GroupBy(p => p.PlayerId)
            .Select(g =>
            {
                var user = usersDict.GetValueOrDefault(g.Key);
                return new TournamentExcelRowDto
                {
                    Username = user.Username ?? "Unknown",
                    Name = user.Name ?? "Unknown",
                    RacketCount = g.Count(),
                    TotalPrice = (decimal)g.Sum(p => p.Price)
                };
            })
            .OrderBy(r => r.Username)
            .ToList();

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
    ///   <item><description>Verifica permisos según el rol del usuario.</description></item>
    ///   <item><description>Filtra tipos solicitados contra la lista blanca.</description></item>
    ///   <item><description>Obtiene datos de cada módulo desde el repositorio especializado correspondiente.</description></item>
    ///   <item><description>Consulta el nombre del torneo desde <c>TalleresDbContext</c>.</description></item>
    ///   <item><description>Delega en <c>IExcelArchiveManager.CreateAdvancedExcelAsync</c>.</description></item>
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
            hasAccess = await excelTalleresRepository.IsUserOwnerOfTournamentAsync(userId, tournamentId);
        }

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("No tienes permisos para exportar este torneo");
        }

        var validTypes = types.Count > 0
            ? types.Where(t => ValidTypes.Contains(t.ToLower())).ToList()
            : ValidTypes;

        var data = new ExcelAdvancedDataDto();

        if (validTypes.Contains("users"))
        {
            data.Users = await excelUserRepository.GetUsersByTournamentAsync(tournamentId);
        }

        if (validTypes.Contains("materials"))
        {
            data.Materials = await excelMaterialsRepository.GetMaterialsByTournamentAsync(tournamentId);
        }

        if (validTypes.Contains("cuerdas"))
        {
            data.Cuerdas = await excelMaterialsRepository.GetCuerdasByTournamentAsync(tournamentId);
        }

        if (validTypes.Contains("tournament"))
        {
            var tournamentData = await excelTalleresRepository.GetTournamentByIdAsync(tournamentId);
            if (tournamentData != null)
            {
                data.Tournament.Add(tournamentData);
            }
        }

        if (validTypes.Contains("pedidos"))
        {
            var (pedidos, lineas) = await excelPedidosRepository.GetPedidosWithLineasAsync(tournamentId);
            data.Pedidos = pedidos;
            data.PedidoLineas = lineas;
        }

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
    ///   <item><description>Filtra los tipos solicitados contra la lista blanca.</description></item>
    ///   <item><description>Lee y parsea el archivo Excel mediante <c>IExcelArchiveManager.ReadExcelAsync</c>.</description></item>
    ///   <item><description>Importa secuencialmente cada módulo según los tipos solicitados.</description></item>
    ///   <item><description>Retorna un <see cref="ExcelImportResultDto"/> con contadores y errores.</description></item>
    /// </list>
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
            hasAccess = await excelTalleresRepository.IsUserOwnerOfTournamentAsync(userId, tournamentId);
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
