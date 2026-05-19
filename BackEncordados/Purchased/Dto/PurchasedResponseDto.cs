using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Dto;

public record PurchasedResponseDto(
    Ulid Id,
    Ulid TournamentId,
    UserResponseDto Player,
    UserResponseDto Encorder,
    string Machine,
    string Comments,
    string PayStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    double Price,
    List<PedidoLineaResponseDto> Lineas
);