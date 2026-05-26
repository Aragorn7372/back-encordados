using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;

namespace BackEncordados.Materials.Model;

/// <summary>
/// Entidad que representa un material de encordado (grips, overgrips, amortiguadores, etc.)
/// en el inventario del sistema.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="ITimestamped"/> para auditoría de fechas de creación
/// y modificación. Soporta soft-delete (<see cref="IsDeleted"/>) e imágenes
/// almacenadas en Cloudinary.</para>
/// <para><b>Propiedades:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <description>Tipo</description>
///     <description>Descripción</description>
///   </listheader>
///   <item>
///     <term><c>Id</c></term>
///     <description>long</description>
///     <description>Identificador numérico autoincremental (PK).</description>
///   </item>
///   <item>
///     <term><c>TournamentId</c></term>
///     <description>Ulid</description>
///     <description>FK al torneo al que pertenece el material.</description>
///   </item>
///   <item>
///     <term><c>Marca</c></term>
///     <description>string</description>
///     <description>Marca del material (ej: "Wilson", "Babolat").</description>
///   </item>
///   <item>
///     <term><c>Modelo</c></term>
///     <description>string</description>
///     <description>Modelo o nombre específico del producto.</description>
///   </item>
///   <item>
///     <term><c>Stock</c></term>
///     <description>int</description>
///     <description>Cantidad disponible en inventario.</description>
///   </item>
///   <item>
///     <term><c>Precio</c></term>
///     <description>double</description>
///     <description>Precio unitario en la moneda local.</description>
///   </item>
///   <item>
///     <term><c>Type</c></term>
///     <description><see cref="MaterialType"/></description>
///     <description>Tipo de material (Grip, Overgrip, LeadTape, etc.).</description>
///   </item>
///   <item>
///     <term><c>ImageUrl</c></term>
///     <description>string</description>
///     <description>URL de la imagen (Cloudinary o default).</description>
///   </item>
///   <item>
///     <term><c>CloudinaryPublicId</c></term>
///     <description>string?</description>
///     <description>ID público en Cloudinary para gestión de imágenes.</description>
///   </item>
///   <item>
///     <term><c>IsDeleted</c></term>
///     <description>bool</description>
///     <description>Soft-delete: false = activo, true = eliminado lógicamente.</description>
///   </item>
///   <item>
///     <term><c>CreatedAt</c></term>
///     <description>DateTime</description>
///     <description>Fecha de creación en UTC (init-only).</description>
///   </item>
///   <item>
///     <term><c>UpdatedAt</c></term>
///     <description>DateTime</description>
///     <description>Fecha de última modificación en UTC (init-only).</description>
///   </item>
/// </list>
/// </remarks>
public class Material : ITimestamped
{
    /// <summary>Identificador numérico autoincremental del material.</summary>
    public long Id { get; set; }

    /// <summary>ID del torneo al que pertenece el material (FK).</summary>
    public Ulid TournamentId { get; set; }

    /// <summary>Marca del material (ej: "Wilson", "Babolat", "Yonex").</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo o nombre específico del producto.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en inventario.</summary>
    public int Stock { get; set; }

    /// <summary>Precio unitario en la moneda local.</summary>
    public double Precio { get; set; }

    /// <summary>Tipo de material según enum <see cref="MaterialType"/>.</summary>
    public MaterialType Type { get; set; } = MaterialType.Grip;

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>URL de la imagen del material (Cloudinary o imagen por defecto).</summary>
    public string ImageUrl { get; set; }=CloudinaryConstants.DEFAULT_IMAGE_MATERIALES;
    
    /// <summary>ID público en Cloudinary para gestión de la imagen.</summary>
    public string? CloudinaryPublicId { get; set; }

    /// <summary>Indica si el material fue eliminado lógicamente (soft-delete).</summary>
    public bool IsDeleted { get; set; }=false;
}