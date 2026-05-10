using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Mapper;

public static class PurchasedMapper
{
    public static PedidoLineaResponseDto ToDto(this PedidoLinea linea)
    {
        return new PedidoLineaResponseDto
        (
            Id: linea.Id,
            RaquetModel: linea.RaquetModel,
            Nudos: linea.Nudos,
            DateString: linea.DateString,
            Logotype: linea.Logotype,
            Color: linea.Color,
            Status: linea.Status,
            StringSetup: linea.StringSetup
        );
    }

    public static PurchasedResponseDto ToDto(this Pedidos pedido, UserResponseDto playerDto, UserResponseDto encorderDto)
    {
        return new PurchasedResponseDto
        (
            Id: pedido.Id,
            TournamentId: pedido.TournamentId,
            Player: playerDto,
            Encorder: encorderDto,
            Machine: pedido.Machine,
            Comments: pedido.Comments,
            PayStatus: pedido.PayStatus.ToString(),
            CreatedAt: pedido.CreatedAt,
            UpdatedAt: pedido.UpdatedAt,
            Lineas: pedido.Lineas.Select(l => l.ToDto()).ToList()
        );
    }

    public static Pedidos ToEntity(this PurchasedRequestDto dto, Ulid playerId, Ulid encorderId)
    {
        var pedido = new Pedidos
        {
            Id = Ulid.NewUlid(),
            TournamentId = dto.TournamentId,
            PlayerId = playerId,
            AssignedTo = encorderId,
            Machine = dto.Machine,
            Comments = dto.Comments,
            PayStatus = Enum.Parse<PaymentStatus>(dto.PayStatus, true),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lineas = new List<PedidoLinea>()
        };

        foreach (var lineaDto in dto.Lineas)
        {
            pedido.Lineas.Add(lineaDto.ToEntity(pedido.Id));
        }

        return pedido;
    }

    public static PedidoLinea ToEntity(this PedidoLineaRequestDto dto, Ulid pedidoId)
    {
        return new PedidoLinea
        {
            Id = Ulid.NewUlid(),
            PedidoId = pedidoId,
            RaquetModel = dto.RaquetModel,
            Nudos = dto.Nudos,
            DateString = dto.DateString,
            Logotype = dto.Logotype,
            Color = dto.Color,
            Status = Status.PENDING,
            StringSetup = dto.StringSetup.ToModel(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static PedidoLinea ToEntity(this PedidoLineaPatchDto dto, PedidoLinea existing)
    {
        if (dto.RaquetModel != null) existing.RaquetModel = dto.RaquetModel;
        if (dto.Nudos.HasValue) existing.Nudos = dto.Nudos.Value;
        if (dto.DateString.HasValue) existing.DateString = dto.DateString.Value;
        if (dto.Logotype.HasValue) existing.Logotype = dto.Logotype.Value;
        if (dto.Color != null) existing.Color = dto.Color;
        if (dto.Status != null) existing.Status = Enum.Parse<Status>(dto.Status, true);
        if (dto.StringSetup != null) existing.StringSetup = dto.StringSetup.ToModel();
        existing.UpdatedAt = DateTime.UtcNow;
        return existing;
    }

    public static StringSetup ToModel(this StringSetupDto stringSetupDto)
    {
        return new StringSetup
        {
            StringV = stringSetupDto.StringV,
            TensionV = stringSetupDto.TensionV,
            PreStetchV = stringSetupDto.PreStetchV,
            StringH = stringSetupDto.StringH,
            TensionH = stringSetupDto.TensionH,
            PreStetchH = stringSetupDto.PreStetchH
        };
    }
}