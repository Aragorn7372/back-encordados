using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Strings;

/// <summary>
/// DTO de entrada para crear una cuerda en el inventario.
/// </summary>
/// <remarks>
/// <para>Se utiliza en el endpoint POST del <c>CuerdasController</c>
/// para recibir los datos de la cuerda.</para>
/// <para><b>Validaciones (DataAnnotations):</b></para>
/// <list type="bullet">
///   <item><description><c>Marca</c> — obligatorio, entre 1 y 100 caracteres.</description></item>
///   <item><description><c>TournamentId</c> — obligatorio.</description></item>
///   <item><description><c>Modelo</c> — obligatorio, entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Stock</c> — obligatorio, rango 0 a <see cref="int.MaxValue"/> (default -1).</description></item>
///   <item><description><c>Precio</c> — obligatorio, rango 0.1 a <see cref="double.MaxValue"/> (default -1).</description></item>
///   <item><description><c>StringFormat</c> — obligatorio, entre 1 y 100 caracteres (string del enum).</description></item>
///   <item><description><c>StringsType</c> — obligatorio, entre 1 y 100 caracteres (string del enum).</description></item>
///   <item><description><c>Calibre</c> — obligatorio, rango 0 a <see cref="double.MaxValue"/>.</description></item>
///   <item><description><c>Imagen</c> — opcional, archivo <c>IFormFile</c>.</description></item>
/// </list>
/// </remarks>
public class CuerdaRequestDto {
    /// <summary>Marca de la cuerda (ej: "Luxilon", "Babolat"). Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    /// <summary>ID del torneo al que pertenece. Obligatorio.</summary>
    [Required]
    public Ulid TournamentId { get; set; }

    /// <summary>Modelo o nombre de la cuerda. Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible (default -1 = no medido). Obligatorio, >= 0.</summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; } = -1;

    /// <summary>Precio unitario (default -1 = no definido). Obligatorio, >= 0.1.</summary>
    [Required]
    [Range(0.1, double.MaxValue)]
    public double Precio { get; set; } = -1;

    /// <summary>Formato de venta como string ("Reel" o "Set"). Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string StringFormat { get; set; } = string.Empty;

    /// <summary>Tipo de cuerda como string ("Polyester", "Multifilament", etc.). Obligatorio, 1-100 caracteres.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string StringsType { get; set; } =string.Empty;

    /// <summary>Calibre o grosor en milímetros (ej: 1.25). Obligatorio, >= 0.</summary>
    [Required]
    [Range(0, double.MaxValue)]
    public double Calibre { get; set; }

    /// <summary>Imagen opcional enviada como archivo (<c>IFormFile</c>).</summary>
    public IFormFile? Imagen { get; set; }
}