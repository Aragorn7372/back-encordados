using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de solicitud para actualización parcial de un pedido.
/// </summary>
/// <remarks>
/// <para>Todos los campos son opcionales. Permite modificar la máquina asignada,
/// los comentarios y el estado de pago del pedido.</para>
/// <para><see cref="Machine"/> debe tener entre 1 y 100 caracteres si se proporciona.
/// <see cref="Comments"/> debe tener entre 1 y 500 caracteres si se proporciona.</para>
/// </remarks>
public record PurchasedPatchDto
{
    /// <summary>Nueva máquina asignada al pedido (opcional). Entre 1 y 100 caracteres.</summary>
    [MinLength(1, ErrorMessage = "La maquina a usar debe tener entre 1 y 100 caracteres")]
    [MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 100 caracteres")]
    public string? Machine { get; init; }

    /// <summary>Nuevos comentarios del pedido (opcional). Entre 1 y 500 caracteres.</summary>
    [MinLength(1, ErrorMessage = "Los comentarios deben tener entre 1 y 500 caracteres")]
    [MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 500 caracteres")]
    public string? Comments { get; init; }

    /// <summary>Nuevo estado de pago (opcional). Valores típicos: Pendiente, Pagado, etc.</summary>
    public string? PayStatus { get; init; }
}