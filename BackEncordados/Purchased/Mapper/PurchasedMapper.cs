using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Mapper;

public static class PurchasedMapper
{
    public static PurchasedResponseDto ToDto(this Pedidos pedido, UserResponseDto playerDto, UserResponseDto encorderDto)
    {
        return new PurchasedResponseDto
        (
            Id: pedido.Id,
            TypeString: pedido.TypeString,
            TypeWork: pedido.TypeWork.ToString(),
            DateString: pedido.DateString,
            Logotype: pedido.Logotype,
            RaquetModel: pedido.RaquetModel,
            Price: pedido.Price,
            Nudos: pedido.Nudos,
            Machine: pedido.Machine,
            Player: playerDto,
            Encorder: encorderDto,
            Comments: pedido.Comments,
            PayStatus: pedido.PayStatus.ToString(),
            Status: pedido.Status.ToString(),
            StringSetup: pedido.StringSetup
            );
    }

    public static Pedidos ToEntity(this PurchasedRequestDto pedido, Guid player, Guid encorder)
    {
        return new Pedidos
        {
            TypeString = pedido.TypeString,
            TypeWork = Enum.Parse<TypePuchase>(pedido.TypeWork, true),
            DateString = pedido.DateString,
            Logotype = pedido.Logotype,
            RaquetModel = pedido.RaquetModel,
            Price = pedido.Price,
            Nudos = pedido.Nudos,
            PlayerId = player,
            AssignedTo = encorder,
            Machine = pedido.Machine,
            Comments = pedido.Comments,
            PayStatus = Enum.Parse<PaymentStatus>(pedido.PayStatus, true),
            Status = Enum.Parse<Status>(pedido.Status, true),
            StringSetup = pedido.StringSetup.ToModel()
        };
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