using BackEncordados.Materials.Model;

namespace BackEncordados.Materials.Dto.Materials;

/// <summary>
/// DTO de salida que representa un material del inventario para las respuestas de la API.
/// </summary>
/// <remarks>
/// <para>Se utiliza en los endpoints GET del <c>MaterialsController</c>
/// para devolver los datos serializados del material al cliente.</para>
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
///     <description>ID numérico del material.</description>
///   </item>
///   <item>
///     <term><c>TournamentId</c></term>
///     <description>Ulid</description>
///     <description>ID del torneo asociado.</description>
///   </item>
///   <item>
///     <term><c>Marca</c></term>
///     <description>string</description>
///     <description>Marca del material.</description>
///   </item>
///   <item>
///     <term><c>Modelo</c></term>
///     <description>string</description>
///     <description>Modelo del material.</description>
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
///     <term><c>MaterialType</c></term>
///     <description>string</description>
///     <description>Tipo de material como string.</description>
///   </item>
///   <item>
///     <term><c>ImageUrl</c></term>
///     <description>string</description>
///     <description>URL de la imagen.</description>
///   </item>
/// </list>
/// </remarks>
public record MaterialResponseDto(
    /// <summary>Identificador numérico del material.</summary>
    long Id,

    /// <summary>ID del torneo al que pertenece.</summary>
    Ulid TournamentId,

    /// <summary>Marca del material.</summary>
    string Marca,

    /// <summary>Modelo o nombre del producto.</summary>
    string Modelo,

    /// <summary>Cantidad disponible en inventario.</summary>
    int Stock,

    /// <summary>Precio unitario en moneda local.</summary>
    double Precio,

    /// <summary>Tipo de material como string (ej: "Grip", "Overgrip").</summary>
    string MaterialType,

    /// <summary>URL de la imagen del material.</summary>
    string ImageUrl
);