using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO que define la configuración de cuerdas para una línea de pedido.
/// </summary>
/// <remarks>
/// <para>Especifica los tipos de cuerda vertical y horizontal, sus tensiones (en kg) y
/// porcentajes de pre-estirado (0-20%).</para>
/// <para>La cuerda vertical es obligatoria; la horizontal es opcional (para encordados de 2 cuerdas).</para>
/// <para><b>Rangos válidos:</b></para>
/// <list type="bullet">
///   <item><description>Tensión: 5 a 40 kg.</description></item>
///   <item><description>Pre-estirado: 0 a 20%.</description></item>
///   <item><description>Nombre cuerda: 1 a 100 caracteres.</description></item>
/// </list>
/// </remarks>
public class StringSetupDto
{
    /// <summary>Nombre del cordaje vertical. Entre 1 y 100 caracteres.</summary>
    [Required(ErrorMessage = "El nombre del cordaje vertical es obligatorio")]
    [MinLength(1, ErrorMessage = "El nombre del cordaje vertical debe tener entre 1 y 100 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre del cordaje vertical no puede superar los 100 caracteres")]
    public string StringV { get; init; } = string.Empty;

    /// <summary>Tensión vertical en kg. Entre 5 y 40.</summary>
    [Required(ErrorMessage = "La tension es obligatoria")]
    [Range(5, 40, ErrorMessage = "La tensión vertical debe estar entre 5 y 40kg")]
    public double TensionV { get; init; }

    /// <summary>Pre-estirado vertical en porcentaje. Entre 0 y 20.</summary>
    [Required(ErrorMessage = "El pre strench es obligatorio")]
    [Range(0, 20, ErrorMessage = "El pre-estirado no puede ser negativo ni mayor al 20%")]
    public short PreStetchV { get; init; }

    /// <summary>Nombre del cordaje horizontal (opcional). Máximo 100 caracteres.</summary>
    [MaxLength(100, ErrorMessage = "El nombre horizontal no puede superar los 100 caracteres")]
    public string StringH { get; init; } = string.Empty;

    /// <summary>Tensión horizontal en kg (opcional). Entre 5 y 40.</summary>
    [Range(5, 40, ErrorMessage = "La tensión horizontal debe estar entre 5 y 40kg")]
    public double TensionH { get; init; }

    /// <summary>Pre-estirado horizontal en porcentaje (opcional). Entre 0 y 20.</summary>
    [Range(0, 20, ErrorMessage = "El pre-estirado horizontal debe estar entre 0 y 20%")]
    public short PreStetchH { get; init; }
}