using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Materials.Dto.Materials;

/// <summary>
/// DTO de entrada para actualización completa (PUT) de un material.
/// </summary>
/// <remarks>
/// <para>A diferencia de un PATCH típico, este DTO requiere todos los campos
/// obligatorios para reemplazar completamente el material existente.</para>
/// <para>Se utiliza en el endpoint PUT del <c>MaterialsController</c>.</para>
/// <para><b>Validaciones:</b></para>
/// <list type="bullet">
///   <item><description><c>Marca</c> — entre 1 y 100 caracteres (no requerido por anotación, pero el servicio valida).</description></item>
///   <item><description><c>Modelo</c> — entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Stock</c> — entero (-1 por defecto = no tocar el stock actual).</description></item>
///   <item><description><c>Precio</c> — double (-1 por defecto = no tocar el precio actual).</description></item>
///   <item><description><c>Type</c> — string del enum, entre 1 y 100 caracteres.</description></item>
///   <item><description><c>Imagen</c> — opcional, archivo <c>IFormFile</c>.</description></item>
/// </list>
/// </remarks>
public class MaterialPatchDto
{
    /// <summary>Marca del material (opcional por validación, 1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo del material (opcional, 1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en inventario (-1 por defecto = no tocar; 0 es un valor válido).</summary>
    public int Stock { get; set; } = -1;

    /// <summary>Precio unitario (-1 por defecto = no tocar).</summary>
    public double Precio { get; set; } = -1;

    /// <summary>Tipo de material como string (1-100 caracteres).</summary>
    [MinLength(1)]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>Imagen opcional enviada como archivo.</summary>
    public IFormFile? Imagen { get; set; }
}