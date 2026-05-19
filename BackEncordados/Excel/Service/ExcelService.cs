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
    private static readonly List<string> ValidTypes = new() { "users", "materials", "cuerdas", "tournament", "pedidos" };

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