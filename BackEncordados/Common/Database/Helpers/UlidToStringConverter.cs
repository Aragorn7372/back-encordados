using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Helpers;

public class UlidToStringConverter : ValueConverter<Ulid?, string?>
{
    public UlidToStringConverter()
        : base(
            v => v.HasValue ? v.Value.ToString() : null,
            v => string.IsNullOrEmpty(v) ? null : Ulid.Parse(v))
    {
    }
}

public class UlidToStringConverterNonNullable : ValueConverter<Ulid, string>
{
    private static string ConvertToDb(Ulid v) => v.ToString();
    private static Ulid ConvertFromDb(string v) => string.IsNullOrEmpty(v) ? throw new ArgumentException("Value cannot be null or empty", nameof(v)) : Ulid.Parse(v);

    public UlidToStringConverterNonNullable()
        : base(v => ConvertToDb(v), v => ConvertFromDb(v))
    {
    }
}