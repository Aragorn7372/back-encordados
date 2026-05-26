using FluentAssertions;
using BackEncordados.Common.Database.Helpers;

namespace TestEncordados.Unit.Common.Database;

public class UlidToStringConverterTests
{
    [Test]
    public void ConvertToProvider_NullUlid_ReturnsNull()
    {
        var converter = new UlidToStringConverter();
        var toProvider = converter.ConvertToProviderExpression.Compile();

        toProvider(null).Should().BeNull();
    }

    [Test]
    public void ConvertToProvider_ValidUlid_ReturnsString()
    {
        var converter = new UlidToStringConverter();
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var ulid = Ulid.NewUlid();

        toProvider(ulid).Should().Be(ulid.ToString());
    }

    [Test]
    public void ConvertFromProvider_NullString_ReturnsNull()
    {
        var converter = new UlidToStringConverter();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        fromProvider(null).Should().BeNull();
    }

    [Test]
    public void ConvertFromProvider_EmptyString_ReturnsNull()
    {
        var converter = new UlidToStringConverter();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        fromProvider("").Should().BeNull();
    }

    [Test]
    public void ConvertFromProvider_ValidString_ReturnsUlid()
    {
        var converter = new UlidToStringConverter();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();
        var ulid = Ulid.NewUlid();

        fromProvider(ulid.ToString()).Should().Be(ulid);
    }

    [Test]
    public void ConvertNonNullable_ToProvider_ReturnsString()
    {
        var converter = new UlidToStringConverterNonNullable();
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var ulid = Ulid.NewUlid();

        toProvider(ulid).Should().Be(ulid.ToString());
    }

    [Test]
    public void ConvertNonNullable_FromProvider_ValidString_ReturnsUlid()
    {
        var converter = new UlidToStringConverterNonNullable();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();
        var ulid = Ulid.NewUlid();

        fromProvider(ulid.ToString()).Should().Be(ulid);
    }

    [Test]
    public void ConvertNonNullable_FromProvider_NullString_Throws()
    {
        var converter = new UlidToStringConverterNonNullable();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        FluentActions.Invoking(() => fromProvider(null!)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void ConvertNonNullable_FromProvider_EmptyString_Throws()
    {
        var converter = new UlidToStringConverterNonNullable();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        FluentActions.Invoking(() => fromProvider("")).Should().Throw<ArgumentException>();
    }
}
