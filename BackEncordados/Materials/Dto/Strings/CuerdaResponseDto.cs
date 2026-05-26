namespace BackEncordados.Materials.Dto.Strings;

/// <summary>
/// DTO de salida que representa una cuerda del inventario para las respuestas de la API.
/// </summary>
/// <remarks>
/// <para>Se utiliza en los endpoints GET del <c>CuerdasController</c>
/// para devolver los datos serializados de la cuerda al cliente.</para>
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
///     <description>ID numérico de la cuerda.</description>
///   </item>
///   <item>
///     <term><c>TournamentId</c></term>
///     <description>Ulid</description>
///     <description>ID del torneo asociado.</description>
///   </item>
///   <item>
///     <term><c>Marca</c></term>
///     <description>string</description>
///     <description>Marca de la cuerda.</description>
///   </item>
///   <item>
///     <term><c>Modelo</c></term>
///     <description>string</description>
///     <description>Modelo de la cuerda.</description>
///   </item>
///   <item>
///     <term><c>Stock</c></term>
///     <description>int</description>
///     <description>Cantidad disponible.</description>
///   </item>
///   <item>
///     <term><c>Precio</c></term>
///     <description>double</description>
///     <description>Precio unitario.</description>
///   </item>
///   <item>
///     <term><c>StringFormat</c></term>
///     <description>string</description>
///     <description>Formato "Reel" o "Set".</description>
///   </item>
///   <item>
///     <term><c>StringsType</c></term>
///     <description>string</description>
///     <description>Tipo de cuerda.</description>
///   </item>
///   <item>
///     <term><c>Calibre</c></term>
///     <description>double</description>
///     <description>Grosor en mm.</description>
///   </item>
///   <item>
///     <term><c>ImageUrl</c></term>
///     <description>string</description>
///     <description>URL de la imagen.</description>
///   </item>
/// </list>
/// </remarks>
public record CuerdaResponseDto(
    /// <summary>Identificador numérico de la cuerda.</summary>
    long Id,

    /// <summary>ID del torneo al que pertenece.</summary>
    Ulid TournamentId,

    /// <summary>Marca de la cuerda.</summary>
    string Marca,

    /// <summary>Modelo o nombre del producto.</summary>
    string Modelo,

    /// <summary>Cantidad disponible en inventario.</summary>
    int Stock,

    /// <summary>Precio unitario en moneda local.</summary>
    double Precio,

    /// <summary>Formato de venta como string ("Reel" o "Set").</summary>
    string StringFormat,

    /// <summary>Tipo de cuerda como string ("Polyester", "Multifilament", etc.).</summary>
    string StringsType,

    /// <summary>Calibre o grosor en milímetros.</summary>
    double Calibre,

    /// <summary>URL de la imagen de la cuerda.</summary>
    string ImageUrl
);