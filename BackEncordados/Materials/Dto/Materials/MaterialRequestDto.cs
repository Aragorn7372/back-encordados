using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Materials;

/// <summary>
/// DTO de entrada para crear un material en el inventario.
/// </summary>
/// <remarks>
/// <para>Se utiliza en el endpoint POST de <c>MaterialsController</c>
/// para recibir los datos del material.</para>
/// <para><b>Validaciones (DataAnnotations):</b></para>
/// <list type="bullet">
///   <item><description><c>Marca</c> — obligatorio, entre 1 y 100 caracteres.</description></item>
///   <item><description><c>TournamentId</c> — obligatorio, ID del torneo propietario.</description></item>
///   <item><description><c>Modelo</c> — obligatorio, entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Stock</c> — obligatorio, rango 0 a <see cref="int.MaxValue"/>.</description></item>
///   <item><description><c>Precio</c> — obligatorio, rango 0.1 a <see cref="double.MaxValue"/>.</description></item>
///   <item><description><c>Type</c> — obligatorio, entre 1 y 100 caracteres (string del enum).</description></item>
///   <item><description><c>Imagen</c> — opcional, archivo <c>IFormFile</c>.</description></item>
/// </list>
/// </remarks>
public class MaterialRequestDto
{
    /// <summary>Marca del material (ej: "Wilson", "Babolat"). Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    /// <summary>ID del torneo al que pertenece el material. Obligatorio.</summary>
    [Required]
    public Ulid TournamentId { get; set; }

    /// <summary>Modelo o nombre del producto. Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en inventario. Obligatorio, >= 0.</summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <summary>Precio unitario en moneda local. Obligatorio, >= 0.1.</summary>
    [Required]
    [Range(0.1, double.MaxValue)]
    public double Precio { get; set; }

    /// <summary>Tipo de material como string (ej: "Grip", "Overgrip"). Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>Imagen opcional enviada como archivo (<c>IFormFile</c>).</summary>
    public IFormFile? Imagen { get; set; }
}