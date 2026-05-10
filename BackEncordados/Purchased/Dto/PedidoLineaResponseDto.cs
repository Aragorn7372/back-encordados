using BackEncordados.Purchased.Model;

namespace BackEncordados.Purchased.Dto;

public record PedidoLineaResponseDto(
    Ulid Id,
    string RaquetModel,
    byte Nudos,
    DateTime DateString,
    bool Logotype,
    string Color,
    Status Status,
    StringSetup StringSetup
);