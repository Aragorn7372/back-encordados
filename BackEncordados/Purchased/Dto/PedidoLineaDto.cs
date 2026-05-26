using System.ComponentModel.DataAnnotations;

namespace BackEncordados.Purchased.Dto;

/// <summary>
/// DTO de solicitud para crear una línea de pedido (una raqueta a encordar).
/// </summary>
/// <remarks>
/// <para>Cada línea representa una raqueta individual dentro de un pedido, especificando
/// el modelo, configuración de cuerdas, nudos, fecha de encordado, logotipo y color.</para>
/// <para>Requiere al menos una línea por pedido. La configuración de cuerdas (<see cref="StringSetup"/>)
/// es obligatoria.</para>
/// </remarks>
public class PedidoLineaRequestDto
{
    /// <summary>Modelo de la raqueta. Entre 1 y 200 caracteres.</summary>
    [Required(ErrorMessage = "El modelo de raqueta es obligatorio")]
    [MinLength(1, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    [MaxLength(200, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    public string RaquetModel { get; set; } = string.Empty;

    /// <summary>Número de nudos del encordado. Valor entero entre 0 y 255.</summary>
    [Required(ErrorMessage = "El número de nudos es obligatorio")]
    public byte Nudos { get; init; }

    /// <summary>Fecha programada para el encordado.</summary>
    [Required(ErrorMessage = "La fecha de encordado es obligatoria")]
    public DateTime DateString { get; set; }

    /// <summary>Indica si la raqueta lleva logotipo personalizado.</summary>
    [Required(ErrorMessage = "Debes indicar si lleva logo")]
    public bool Logotype { get; set; }

    /// <summary>Color del encordado. Máximo 100 caracteres.</summary>
    [MaxLength(100, ErrorMessage = "El color no puede superar los 100 caracteres")]
    public string Color { get; set; } = string.Empty;

    /// <summary>Configuración de tensiones y tipos de cuerda (vertical y horizontal).</summary>
    [Required(ErrorMessage = "La configuración de las cuerdas es obligatoria")]
    public StringSetupDto StringSetup { get; init; } = null!;
}

/// <summary>
/// DTO de solicitud para actualización parcial de una línea de pedido.
/// </summary>
/// <remarks>
/// <para>Todos los campos son opcionales. Permite modificar el modelo, nudos, fecha, logotipo,
/// color, estado y configuración de cuerdas de una línea existente.</para>
/// </remarks>
public class PedidoLineaPatchDto
{
    /// <summary>Nuevo modelo de raqueta (opcional).</summary>
    [MinLength(1, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    [MaxLength(200, ErrorMessage = "El modelo debe tener entre 1 y 200 caracteres")]
    public string? RaquetModel { get; set; }

    /// <summary>Nuevo número de nudos (opcional).</summary>
    public byte? Nudos { get; set; }

    /// <summary>Nueva fecha de encordado (opcional).</summary>
    public DateTime? DateString { get; set; }

    /// <summary>Nuevo valor de logotipo (opcional).</summary>
    public bool? Logotype { get; set; }

    /// <summary>Nuevo color (opcional). Máximo 100 caracteres.</summary>
    [MaxLength(100, ErrorMessage = "El color no puede superar los 100 caracteres")]
    public string? Color { get; set; }

    /// <summary>Nuevo estado de la línea (opcional).</summary>
    public string? Status { get; set; }

    /// <summary>Nueva configuración de cuerdas (opcional).</summary>
    public StringSetupDto? StringSetup { get; set; }
}