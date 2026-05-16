using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackEncordados.Common.Database.Helpers;

public class UlidToStringConverter : ValueConverter<Ulid, string>
{
    public UlidToStringConverter()
        : base(
            convertToProviderExpression: v => v.ToString(),
            convertFromProviderExpression: v => Ulid.Parse(v))
    {
    }
}