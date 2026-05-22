using BackEncordados.Infraestructure.Constraints;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;

namespace TestEncordados.Unit.Infrastructure;

public class UlidRouteConstraintTests
{
    private readonly UlidRouteConstraint _sut = new();

    [Test]
    public void Match_WithValidUlid_ReturnsTrue()
    {
        var values = new RouteValueDictionary { { "id", Ulid.NewUlid().ToString() } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeTrue();
    }

    [Test]
    public void Match_WithInvalidString_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", "not-a-ulid" } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    [Test]
    public void Match_WithNonStringValue_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", 12345 } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    [Test]
    public void Match_WithMissingKey_ReturnsFalse()
    {
        var values = new RouteValueDictionary();

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    [Test]
    public void Match_WithEmptyString_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", "" } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    [Test]
    public void Match_WithNullValue_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", null } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }
}
