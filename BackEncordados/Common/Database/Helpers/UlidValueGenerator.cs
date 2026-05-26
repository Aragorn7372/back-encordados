using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace BackEncordados.Common.Database.Helpers;

/// <summary>
/// Generador de valores para EF Core que crea nuevos identificadores <see cref="Ulid"/>
/// para las propiedades de clave primaria tipo <see cref="Ulid"/>.
/// </summary>
/// <remarks>
/// Utiliza <c>Ulid.NewUlid()</c> para generar identificadores ordenables y únicos,
/// a diferencia de los GUIDs tradicionales. Los valores generados no son temporales,
/// por lo que se persisten inmediatamente en la base de datos.
/// </remarks>
public class UlidValueGenerator : ValueGenerator<Ulid>
{
    /// <summary>Genera un nuevo <see cref="Ulid"/> único para la entidad.</summary>
    /// <param name="entry">Entrada de la entidad que está siendo rastreada por el ChangeTracker.</param>
    /// <returns>Nuevo <see cref="Ulid"/> generado.</returns>
    public override Ulid Next(EntityEntry entry) => Ulid.NewUlid();

    /// <summary>Indica que los valores generados no son temporales y deben persistirse.</summary>
    public override bool GeneratesTemporaryValues => false;
}