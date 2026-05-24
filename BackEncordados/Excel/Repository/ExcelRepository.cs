using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class ExcelRepository(
    PedidosDbContext pedidosDbContext,
    UserDbContext userDbContext,
    TalleresDbContext talleresDbContext,
    MaterialsDbContext materialsDbContext
) : IExcelRepository
{
    public async Task<IEnumerable<TournamentExcelRowDto>> GetTournamentDataAsync(Ulid tournamentId)
    {
        var pedidos = await pedidosDbContext.Pedidos
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();

        var playerIds = pedidos.Select(p => p.PlayerId).Distinct().ToList();
        
        var users = await userDbContext.Users
            .Where(u => playerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Username, u.Name });

        var result = pedidos
            .GroupBy(p => p.PlayerId)
            .Select(g =>
            {
                var user = users.GetValueOrDefault(g.Key);
                return new TournamentExcelRowDto
                {
                    Username = user?.Username ?? "Unknown",
                    Name = user?.Name ?? "Unknown",
                    RacketCount = g.Count(),
                    TotalPrice = (decimal)g.Sum(p => p.Price)
                };
            })
            .OrderBy(r => r.Username)
            .ToList();

        return result;
    }

    public async Task<bool> IsUserSupervisorOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.SupervisorList.Contains(userId);
    }

    public async Task<bool> IsUserOwnerOfTournamentAsync(Ulid userId, Ulid tournamentId)
    {
        var tournament = await talleresDbContext.Partidos
            .Where(t => t.Id == tournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return false;

        return tournament.Owner == userId;
    }

    public async Task<ExcelAdvancedDataDto> GetAdvancedDataAsync(Ulid tournamentId, List<string> types)
    {
        var data = new ExcelAdvancedDataDto();

        if (types.Contains("users"))
        {
            var users = await userDbContext.Users
                .AsNoTracking()
                .Where(u => u.TournamentId == tournamentId)
                .ToListAsync();
            
            data.Users = users.Select(u => new ExcelUsersDto
            {
                Id = u.Id.ToString(),
                Username = u.Username,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                TournamentId = u.TournamentId?.ToString()
            }).ToList();
        }

        if (types.Contains("materials"))
        {
            var materials = await materialsDbContext.Materiales
                .AsNoTracking()
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();

            data.Materials = materials.Select(m => new ExcelMaterialsDto
            {
                Id = m.Id,
                TournamentId = m.TournamentId.ToString(),
                Marca = m.Marca,
                Modelo = m.Modelo,
                Stock = m.Stock,
                Precio = m.Precio,
                Type = m.Type.ToString()
            }).ToList();
        }

        if (types.Contains("cuerdas"))
        {
            var cuerdas = await materialsDbContext.Cuerdas
                .AsNoTracking()
                .Where(c => c.TournamentId == tournamentId)
                .ToListAsync();

            data.Cuerdas = cuerdas.Select(c => new ExcelCuerdasDto
            {
                Id = c.Id,
                TournamentId = c.TournamentId.ToString(),
                Marca = c.Marca,
                Modelo = c.Modelo,
                Stock = c.Stock,
                Precio = c.Precio,
                StringFormat = c.StringFormat.ToString(),
                StringsType = c.StringsType.ToString()
            }).ToList();
        }

        if (types.Contains("tournament"))
        {
            var tournament = await talleresDbContext.Partidos
                .Where(t => t.Id == tournamentId)
                .FirstOrDefaultAsync();

            if (tournament != null)
            {
                data.Tournament.Add(new ExcelTournamentDto
                {
                    Id = tournament.Id.ToString(),
                    Owner = tournament.Owner.ToString(),
                    Title = tournament.Title,
                    StartTournament = tournament.StartTournament,
                    EndTournament = tournament.EndTournament,
                    Logotype = tournament.Logotype,
                    WorkersList = string.Join(";", tournament.WorkersList),
                    SupervisorList = string.Join(";", tournament.SupervisorList)
                });
            }
        }

        if (types.Contains("pedidos"))
        {
            var pedidos = await pedidosDbContext.Pedidos
                .Where(p => p.TournamentId == tournamentId)
                .ToListAsync();

            data.Pedidos = pedidos.Select(p => new ExcelPedidosDto
            {
                Id = p.Id.ToString(),
                TournamentId = p.TournamentId.ToString(),
                PlayerId = p.PlayerId.ToString(),
                AssignedTo = p.AssignedTo.ToString(),
                Machine = p.Machine,
                Comments = p.Comments,
                Price = p.Price,
                PayStatus = p.PayStatus.ToString()
            }).ToList();

            var pedidoIds = pedidos.Select(p => p.Id).ToList();
            var lineas = await pedidosDbContext.PedidoLineas
                .Where(l => pedidoIds.Contains(l.PedidoId))
                .ToListAsync();

            data.PedidoLineas = lineas.Select(l => new ExcelPedidoLineasDto
            {
                Id = l.Id.ToString(),
                PedidoId = l.PedidoId.ToString(),
                RaquetModel = l.RaquetModel,
                Nudos = l.Nudos,
                DateString = l.DateString,
                Logotype = l.Logotype,
                Color = l.Color,
                StringV = l.StringSetup?.StringV ?? "",
                TensionV = l.StringSetup?.TensionV ?? 0,
                PreStetchV = l.StringSetup?.PreStetchV ?? 0,
                StringH = l.StringSetup?.StringH ?? "",
                TensionH = l.StringSetup?.TensionH ?? 0,
                PreStetchH = l.StringSetup?.PreStetchH ?? 0,
                Status = l.Status.ToString()
            }).ToList();
        }

        return data;
    }
}