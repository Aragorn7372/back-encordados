using BackEncordados.Infraestructure.Constraints;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;

namespace TestEncordados.Unit.Infrastructure;

/// <summary>
/// Tests unitarios para <see cref="UlidRouteConstraint"/>, la constraint de ruta
/// que valida que un parámetro de ruta sea un ULID válido en ASP.NET Core.
/// </summary>
/// <remarks>
/// <para>Verifica la implementación de <c>IRouteConstraint.Match</c> cubriendo
/// todos los escenarios posibles:</para>
/// <list type="bullet">
///   <item><description>ULID válido — debe retornar <c>true</c>.</description></item>
///   <item><description>String inválido — debe retornar <c>false</c>.</description></item>
///   <item><description>Tipo no-string (int) — debe retornar <c>false</c>.</description></item>
///   <item><description>Clave ausente en el diccionario — debe retornar <c>false</c>.</description></item>
///   <item><description>String vacío — debe retornar <c>false</c>.</description></item>
///   <item><description>Valor nulo — debe retornar <c>false</c>.</description></item>
/// </list>
/// <para>Usa <c>FluentAssertions</c> para las aserciones y NUnit como framework de测试.</para>
/// </remarks>
public class UlidRouteConstraintTests
{
    /// <summary>
    /// Instancia SUT (System Under Test) del constraint bajo prueba.
    /// </summary>
    private readonly UlidRouteConstraint _sut = new();

    /// <summary>
    /// Verifica que un ULID válido en formato string es aceptado por la constraint.
    /// </summary>
    [Test]
    public void Match_WithValidUlid_ReturnsTrue()
    {
        var values = new RouteValueDictionary { { "id", Ulid.NewUlid().ToString() } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un string que no es un ULID válido es rechazado por la constraint.
    /// </summary>
    [Test]
    public void Match_WithInvalidString_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", "not-a-ulid" } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que un valor numérico (tipo incorrecto) es rechazado por la constraint.
    /// </summary>
    [Test]
    public void Match_WithNonStringValue_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", 12345 } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que cuando la clave solicitada no existe en el diccionario,
    /// la constraint retorna <c>false</c>.
    /// </summary>
    [Test]
    public void Match_WithMissingKey_ReturnsFalse()
    {
        var values = new RouteValueDictionary();

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que un string vacío es rechazado por la constraint
    /// (un ULID debe tener 26 caracteres).
    /// </summary>
    [Test]
    public void Match_WithEmptyString_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", "" } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que un valor nulo es rechazado por la constraint
    /// (el método <c>Ulid.TryParse</c> retorna <c>false</c> para nulos).
    /// </summary>
    [Test]
    public void Match_WithNullValue_ReturnsFalse()
    {
        var values = new RouteValueDictionary { { "id", null } };

        var result = _sut.Match(null, null, "id", values, RouteDirection.IncomingRequest);

        result.Should().BeFalse();
    }
}
