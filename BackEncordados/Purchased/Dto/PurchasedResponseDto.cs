using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Dto;

public record PurchasedResponseDto(
    Guid Id,
    string TypeString,
    string TypeWork,
    DateTime DateString,
    bool Logotype,
    string RaquetModel,
    double Price,
    byte Nudos,
    UserResponseDto Player, 
    UserResponseDto Encorder,
    string Machine,
    string Comments,
    string PayStatus,
    string Status,
    StringSetup StringSetup
    );