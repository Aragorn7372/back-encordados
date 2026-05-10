using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

public record PurchasedPatchDto
{
    [MinLength(1, ErrorMessage = "La maquina a usar debe tener entre 1 y 100 caracteres")]
    [MaxLength(100, ErrorMessage = "La maquina a usar debe tener entre 100 caracteres")]
    public string? Machine { get; init; }

    [MinLength(1, ErrorMessage = "Los comentarios deben tener entre 1 y 500 caracteres")]
    [MaxLength(500, ErrorMessage = "Los comentarios deben tener entre 500 caracteres")]
    public string? Comments { get; init; }

    public string? PayStatus { get; init; }
}