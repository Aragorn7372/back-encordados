using BackEncordados.Purchased.Model;
using BackEncordados.Usuarios.Dto;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de respuesta con la información completa de un pedido de encordado.
/// </summary>
/// <remarks>
/// <para>Incluye los datos del pedido, los usuarios involucrados (jugador y encordador),
/// la máquina asignada, comentarios, estado de pago, fechas de creación/actualización,
/// precio total y la lista detallada de líneas de pedido.</para>
/// <para>Se utiliza como respuesta principal del módulo Purchased.</para>
/// </remarks>
/// <param name="Id">Identificador ULID del pedido.</param>
/// <param name="TournamentId">ULID del torneo asociado.</param>
/// <param name="Player">Datos públicos del jugador que realizó el encargo.</param>
/// <param name="Encorder">Datos públicos del encordador asignado.</param>
/// <param name="Machine">Máquina utilizada para el encordado.</param>
/// <param name="Comments">Comentarios adicionales del pedido.</param>
/// <param name="PayStatus">Estado de pago actual.</param>
/// <param name="CreatedAt">Fecha y hora de creación del pedido (UTC).</param>
/// <param name="UpdatedAt">Fecha y hora de la última actualización (UTC).</param>
/// <param name="Price">Precio total del pedido.</param>
/// <param name="Lineas">Lista de líneas de pedido (raquetas) con su configuración.</param>
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