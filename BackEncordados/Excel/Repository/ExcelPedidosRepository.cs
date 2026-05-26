using BackEncordados.Common.Database.Config;
using BackEncordados.Excel.Dto;
using BackEncordados.Purchased.Model;
using Microsoft.EntityFrameworkCore;

namespace BackEncordados.Excel.Repository;

public class ExcelPedidosRepository(PedidosDbContext pedidosDbContext) : IExcelPedidosRepository
{
    public async Task<List<Pedidos>> GetPedidosByTournamentAsync(Ulid tournamentId)
    {
        return await pedidosDbContext.Pedidos
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();
    }

    public async Task<(List<ExcelPedidosDto> Pedidos, List<ExcelPedidoLineasDto> Lineas)> GetPedidosWithLineasAsync(Ulid tournamentId)
    {
        var pedidos = await pedidosDbContext.Pedidos
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();

        var pedidosDto = pedidos.Select(p => new ExcelPedidosDto
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

        var lineasDto = lineas.Select(l => new ExcelPedidoLineasDto
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

        return (pedidosDto, lineasDto);
    }
}
