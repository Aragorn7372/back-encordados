using BackEncordados.Common.Database.Helpers;
using BackEncordados.Common.Service.Cloudinary;

namespace BackEncordados.Materials.Model;

/// <summary>
/// Entidad que representa una cuerda de encordado en el inventario del sistema.
/// </summary>
/// <remarks>
/// <para>Implementa <see cref="ITimestamped"/> para auditoría de fechas.
/// A diferencia de <see cref="Material"/>, las cuerdas tienen propiedades
/// técnicas específicas: calibre, formato y tipo de cordaje.</para>
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
///     <description>FK al torneo al que pertenece la cuerda.</description>
///   </item>
///   <item>
///     <term><c>Marca</c></term>
///     <description>string</description>
///     <description>Marca de la cuerda (ej: "Luxilon", "Babolat").</description>
///   </item>
///   <item>
///     <term><c>Modelo</c></term>
///     <description>string</description>
///     <description>Modelo o nombre específico de la cuerda.</description>
///   </item>
///   <item>
///     <term><c>Stock</c></term>
///     <description>int</description>
///     <description>Cantidad disponible (default -1 = no medido).</description>
///   </item>
///   <item>
///     <term><c>Precio</c></term>
///     <description>double</description>
///     <description>Precio unitario (default -1 = no definido).</description>
///   </item>
///   <item>
///     <term><c>StringFormat</c></term>
///     <description><see cref="FormatoCuerda"/></description>
///     <description>Formato de venta: Reel (bobina) o Set (juego).</description>
///   </item>
///   <item>
///     <term><c>StringsType</c></term>
///     <description><see cref="StringsType"/></description>
///     <description>Tipo de cuerda: Polyester, Multifilament, NaturalGut, etc.</description>
///   </item>
///   <item>
///     <term><c>Calibre</c></term>
///     <description>double</description>
///     <description>Grosor de la cuerda en milímetros (ej: 1.25, 1.30).</description>
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
public class Cuerdas: ITimestamped
{
    /// <summary>Identificador numérico autoincremental de la cuerda.</summary>
    public long Id { get; set; }

    /// <summary>ID del torneo al que pertenece la cuerda (FK).</summary>
    public Ulid TournamentId { get; set; }

    /// <summary>Marca de la cuerda (ej: "Luxilon", "Babolat", "Wilson").</summary>
    public string Marca { get; set; } = string.Empty;

    /// <summary>Modelo o nombre específico de la cuerda.</summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>Cantidad disponible en inventario (default -1 = no medido).</summary>
    public int Stock { get; set; } = -1;

    /// <summary>Precio unitario (default -1 = no definido).</summary>
    public double Precio { get; set; } = -1;

    /// <summary>Formato de venta: Reel (bobina) o Set (juego individual).</summary>
    public FormatoCuerda StringFormat { get; set; } = FormatoCuerda.Reel;

    /// <summary>Tipo de cuerda (Polyester, Multifilament, NaturalGut, SyntheticGut, Hybrid).</summary>
    public StringsType StringsType { get; set; } = StringsType.Polyester;

    /// <summary>Calibre o grosor de la cuerda en milímetros (ej: 1.25, 1.30).</summary>
    public double Calibre { get; set; }
    
    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Indica si la cuerda fue eliminada lógicamente (soft-delete).</summary>
    public bool IsDeleted { get; set; }=false;

    /// <summary>URL de la imagen de la cuerda (Cloudinary o imagen por defecto).</summary>
    public string ImageUrl { get; set; }=CloudinaryConstants.DEFAULT_IMAGE_MATERIALES;

    /// <summary>ID público en Cloudinary para gestión de la imagen.</summary>
    public string? CloudinaryPublicId { get; set; }
}