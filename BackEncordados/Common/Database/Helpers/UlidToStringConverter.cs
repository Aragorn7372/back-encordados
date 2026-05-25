using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Helpers;

/// <summary>
/// ValueConverter de EF Core que convierte <see cref="Ulid"/> nullable a <c>string</c> nullable y viceversa.
/// Utilizado para propiedades opcionales como <c>TournamentId</c> en <see cref="User"/>.
/// </summary>
/// <remarks>
/// <para>Transforma <c>null</c> a <c>null</c> en ambos sentidos.</para>
/// <para>Si el valor string no es nulo ni vacío, se parsea con <c>Ulid.Parse()</c>.</para>
/// </remarks>
public class UlidToStringConverter : ValueConverter<Ulid?, string?>
{
    /// <summary>Inicializa el converter con las expresiones de conversión bidireccional.</summary>
    public UlidToStringConverter()
        : base(
            v => v.HasValue ? v.Value.ToString() : null,
            v => string.IsNullOrEmpty(v) ? null : Ulid.Parse(v))
    {
    }
}

/// <summary>
/// ValueConverter de EF Core que convierte <see cref="Ulid"/> no-nullable a <c>string</c> no-nullable y viceversa.
/// Utilizado para propiedades requeridas como <c>Id</c> en todas las entidades del dominio.
/// </summary>
/// <remarks>
/// <para>A diferencia de <see cref="UlidToStringConverter"/>, este converter lanza <see cref="ArgumentException"/>
/// si el valor string recibido desde la base de datos es nulo o vacío.</para>
/// <para>Es el converter usado por defecto en las convenciones globales de <c>ConfigureConventions()</c>
/// en <see cref="MaterialsDbContext"/>, <see cref="PedidosDbContext"/> y <see cref="TalleresDbContext"/>.</para>
/// </remarks>
public class UlidToStringConverterNonNullable : ValueConverter<Ulid, string>
{
    private static string ConvertToDb(Ulid v) => v.ToString();
    private static Ulid ConvertFromDb(string v) => string.IsNullOrEmpty(v) ? throw new ArgumentException("Value cannot be null or empty", nameof(v)) : Ulid.Parse(v);

    /// <summary>Inicializa el converter con las expresiones de conversión bidireccional.</summary>
    public UlidToStringConverterNonNullable()
        : base(v => ConvertToDb(v), v => ConvertFromDb(v))
    {
    }
}