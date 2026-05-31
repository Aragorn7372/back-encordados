using BackEncordados.Excel.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Purchased.Model;
using BackEncordados.Talleres.Model;
using BackEncordados.Usuarios.Model;

namespace BackEncordados.Excel.Mapper;

public static class ExcelMapper
{
    public static ExcelUsersDto ToExcelUsersDto(this User user)
    {
        return new ExcelUsersDto
        {
            Id = user.Id.ToString(),
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            TournamentId = user.TournamentId?.ToString()
        };
    }

    public static ExcelMaterialsDto ToExcelMaterialsDto(this Material material)
    {
        return new ExcelMaterialsDto
        {
            Id = material.Id,
            TournamentId = material.TournamentId.ToString(),
            Marca = material.Marca,
            Modelo = material.Modelo,
            Stock = material.Stock,
            Precio = material.Precio,
            Type = material.Type.ToString()
        };
    }

    public static ExcelCuerdasDto ToExcelCuerdasDto(this Cuerdas cuerda)
    {
        return new ExcelCuerdasDto
        {
            Id = cuerda.Id,
            TournamentId = cuerda.TournamentId.ToString(),
            Marca = cuerda.Marca,
            Modelo = cuerda.Modelo,
            Stock = cuerda.Stock,
            Precio = cuerda.Precio,
            Calibre = cuerda.Calibre,
            StringFormat = cuerda.StringFormat.ToString(),
            StringsType = cuerda.StringsType.ToString()
        };
    }

    public static ExcelTournamentDto ToExcelTournamentDto(this Tournaments tournament)
    {
        return new ExcelTournamentDto
        {
            Id = tournament.Id.ToString(),
            Owner = tournament.Owner.ToString(),
            Title = tournament.Title,
            StartTournament = tournament.StartTournament,
            EndTournament = tournament.EndTournament,
            Logotype = tournament.Logotype,
            WorkersList = string.Join(";", tournament.WorkersList),
            SupervisorList = string.Join(";", tournament.SupervisorList)
        };
    }

    public static ExcelPedidosDto ToExcelPedidosDto(this Pedidos pedido)
    {
        return new ExcelPedidosDto
        {
            Id = pedido.Id.ToString(),
            TournamentId = pedido.TournamentId.ToString(),
            PlayerId = pedido.PlayerId.ToString(),
            AssignedTo = pedido.AssignedTo.ToString(),
            Machine = pedido.Machine,
            Comments = pedido.Comments,
            Price = pedido.Price,
            PayStatus = pedido.PayStatus.ToString()
        };
    }

    public static ExcelPedidoLineasDto ToExcelPedidoLineasDto(this PedidoLinea linea)
    {
        return new ExcelPedidoLineasDto
        {
            Id = linea.Id.ToString(),
            PedidoId = linea.PedidoId.ToString(),
            RaquetModel = linea.RaquetModel,
            Nudos = linea.Nudos,
            DateString = linea.DateString,
            Logotype = linea.Logotype,
            Color = linea.Color,
            StringV = linea.StringSetup?.StringV ?? "",
            TensionV = linea.StringSetup?.TensionV ?? 0,
            PreStetchV = linea.StringSetup?.PreStetchV ?? 0,
            StringH = linea.StringSetup?.StringH ?? "",
            TensionH = linea.StringSetup?.TensionH ?? 0,
            PreStetchH = linea.StringSetup?.PreStetchH ?? 0,
            Status = linea.Status.ToString()
        };
    }

    public static TournamentExcelRowDto ToTournamentExcelRowDto(
        this IGrouping<Ulid, Pedidos> group,
        Dictionary<Ulid, (string Username, string Name)> users)
    {
        var user = users.GetValueOrDefault(group.Key);
        return new TournamentExcelRowDto
        {
            Username = user.Username ?? "Unknown",
            Name = user.Name ?? "Unknown",
            RacketCount = group.Count(),
            TotalPrice = (decimal)group.Sum(p => p.Price)
        };
    }
}
