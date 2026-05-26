using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Strings;

/// <summary>
/// DTO de entrada para actualización completa (PUT) de una cuerda.
/// </summary>
/// <remarks>
/// <para>Se utiliza en el endpoint PUT del <c>CuerdasController</c>
/// para reemplazar completamente los datos de la cuerda existente.</para>
/// <para><b>Validaciones:</b></para>
/// <list type="bullet">
///   <item><description><c>Marca</c> — entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Modelo</c> — entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Stock</c> — entero (default -1).</description></item>
///   <item><description><c>Precio</c> — double (default -1).</description></item>
///   <item><description><c>StringFormat</c> — string del enum, 1-100 caracteres.</description></item>
///   <item><description><c>StringsType</c> — string del enum, 1-100 caracteres.</description></item>
///   <item><description><c>Calibre</c> — double.</description></item>
///   <item><description><c>Imagen</c> — opcional, archivo <c>IFormFile</c>.</description></item>
/// </list>
/// </remarks>
public class CuerdaPatchDto
{
    /// <summary>Marca de la cuerda (1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo de la cuerda (1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible (default -1 = no medido).</summary>
    public int Stock { get; set; } = -1;

    /// <summary>Precio unitario (default -1 = no definido).</summary>
    public double Precio { get; set; } = -1;

    /// <summary>Formato de venta como string ("Reel" o "Set", 1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string StringFormat { get; set; } = string.Empty;

    /// <summary>Tipo de cuerda como string ("Polyester", etc., 1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string StringsType { get; set; } =string.Empty;

    /// <summary>Calibre o grosor en milímetros.</summary>
    public double Calibre { get; set; }

    /// <summary>Imagen opcional enviada como archivo.</summary>
    public IFormFile? Imagen { get; set; }
}