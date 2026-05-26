using BackEncordados.Common.Service.Email;
using FluentAssertions;

namespace TestEncordados.Unit.Common.Service.Email;

public class EmailTemplatesTests
{
    [Test]
    public void CreateBase_ContainsTitleAndContent()
    {
        var result = EmailTemplates.CreateBase("Test Title", "Test Content");

        result.Should().Contain("Test Title");
        result.Should().Contain("Test Content");
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("Encordados");
    }

    [Test]
    public void CreateBaseWithButton_ContainsButtonUrlAndText()
    {
        var result = EmailTemplates.CreateBaseWithButton(
            "Test Title", "Test Content", "https://example.com/action", "Click Me");

        result.Should().Contain("Test Title");
        result.Should().Contain("Test Content");
        result.Should().Contain("https://example.com/action");
        result.Should().Contain("Click Me");
        result.Should().Contain("<a href=");
    }

    [Test]
    public void AccountCreated_ContainsUserInfo()
    {
        var result = EmailTemplates.AccountCreated("JohnDoe", "john@test.com");

        result.Should().Contain("JohnDoe");
        result.Should().Contain("john@test.com");
        result.Should().Contain("Cuenta creada");
        result.Should().Contain("Tu cuenta ha sido creada exitosamente");
    }

    [Test]
    public void PasswordReset_ContainsResetUrlAndExpiry()
    {
        var result = EmailTemplates.PasswordReset("https://example.com/reset?token=abc123", 2);

        result.Should().Contain("https://example.com/reset?token=abc123");
        result.Should().Contain("2 horas");
        result.Should().Contain("Restablecer contraseña");
    }

    [Test]
    public void PasswordReset_DefaultExpiryIsOneHour()
    {
        var result = EmailTemplates.PasswordReset("https://example.com/reset");

        result.Should().Contain("1 hora");
    }

    [Test]
    public void PasswordReset_SingularExpiryText_WhenOneHour()
    {
        var result = EmailTemplates.PasswordReset("https://example.com/reset", 1);

        result.Should().Contain("1 hora");
        result.Should().NotContain("horas");
    }

    [Test]
    public void OrderCancelled_ContainsOrderId()
    {
        var result = EmailTemplates.OrderCancelled("ORD-12345");

        result.Should().Contain("ORD-12345");
        result.Should().Contain("Pedido cancelado");
        result.Should().Contain("Lamentamos informarte");
    }

    [Test]
    public void PaymentConfirmed_ContainsOrderIdAndAmount()
    {
        var result = EmailTemplates.PaymentConfirmed("ORD-999", 150.50);

        result.Should().Contain("ORD-999");
        result.Should().Contain("150");
        result.Should().Contain("€");
        result.Should().Contain("Pago confirmado");
        result.Should().Contain("Importe pagado");
    }

    [Test]
    public void LineaCompleted_ContainsLineAndPedidoInfo()
    {
        var result = EmailTemplates.LineaCompleted("L-001", "P-001", "Pro Staff 97");

        result.Should().Contain("L-001");
        result.Should().Contain("P-001");
        result.Should().Contain("Pro Staff 97");
        result.Should().Contain("Completada");
        result.Should().Contain("Línea completada");
    }

    [Test]
    public void LineaDelivered_ContainsLineAndPedidoInfo()
    {
        var result = EmailTemplates.LineaDelivered("L-002", "P-002", "Pure Aero");

        result.Should().Contain("L-002");
        result.Should().Contain("P-002");
        result.Should().Contain("Pure Aero");
        result.Should().Contain("Entregada");
        result.Should().Contain("Línea entregada");
    }
}
