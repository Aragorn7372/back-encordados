using System.Net;
using System.Text;
using System.Text.Json;
using BackEncordados.Common.Service.WhatsApp;
using BackEncordados.Infraestructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace TestEncordados.Unit.Common.Service.WhatsApp;

public class WhatsAppServiceTests
{
    private Mock<ILogger<WhatsAppService>> _mockLogger = null!;
    private Mock<IHttpClientFactory> _mockHttpClientFactory = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private HttpClient _httpClient = null!;
    private WhatsAppService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<WhatsAppService>>();

        _mockHttpClientFactory
            .Setup(x => x.CreateClient("WhatsApp"))
            .Returns(_httpClient);

        AppConfig.Current = new AppOptions
        {
            WhatsAppEnabled = true,
            WhatsAppPhoneNumberId = "123456789",
            WhatsAppAccessToken = "test_token",
            WhatsAppApiVersion = "v21.0"
        };

        _service = new WhatsAppService(
            _mockLogger.Object,
            _mockHttpClientFactory.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        AppConfig.Current = new AppOptions();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string body = "")
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
    }

    private void SetupHttpException(Exception exception)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private string? GetCapturedRequestBody(HttpRequestMessage? request)
    {
        if (request?.Content == null) return null;
        return request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    #region Disabled service

    [Test]
    public async Task SendMessageAsync_WhenDisabled_ReturnsFalse()
    {
        AppConfig.Current.WhatsAppEnabled = false;

        var result = await _service.SendMessageAsync("+34600000000", "Hola");

        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp service is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendTemplateMessageAsync_WhenDisabled_ReturnsFalse()
    {
        AppConfig.Current.WhatsAppEnabled = false;

        var result = await _service.SendTemplateMessageAsync("+34600000000", "welcome");

        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp service is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendLineaCompletedMessageAsync_WhenDisabled_ReturnsFalse()
    {
        AppConfig.Current.WhatsAppEnabled = false;

        var result = await _service.SendLineaCompletedMessageAsync("+34600000000", "Juan", "Pro Staff", "P-001");

        result.Should().BeFalse();
    }

    [Test]
    public async Task SendLineaCanceledMessageAsync_WhenDisabled_ReturnsFalse()
    {
        AppConfig.Current.WhatsAppEnabled = false;

        var result = await _service.SendLineaCanceledMessageAsync("+34600000000", "Juan", "Pro Staff", "P-001");

        result.Should().BeFalse();
    }

    [Test]
    public async Task SendPedidoCanceledMessageAsync_WhenDisabled_ReturnsFalse()
    {
        AppConfig.Current.WhatsAppEnabled = false;

        var result = await _service.SendPedidoCanceledMessageAsync("+34600000000", "Juan", "P-001", 3);

        result.Should().BeFalse();
    }

    #endregion

    #region Phone validation

    [Test]
    public async Task SendMessageAsync_WhenPhoneEmpty_ReturnsFalse()
    {
        var result = await _service.SendMessageAsync("", "Hola");

        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Phone number is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendMessageAsync_WhenPhoneNull_ReturnsFalse()
    {
        var result = await _service.SendMessageAsync(null!, "Hola");

        result.Should().BeFalse();
    }

    [Test]
    public async Task SendTemplateMessageAsync_WhenPhoneEmpty_ReturnsFalse()
    {
        var result = await _service.SendTemplateMessageAsync("", "welcome");

        result.Should().BeFalse();
    }

    #endregion

    #region HTTP success / error

    [Test]
    public async Task SendMessageAsync_WhenApiSuccess_ReturnsTrue()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.SendMessageAsync("+34600000000", "Hola mundo");

        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp message sent successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendMessageAsync_WhenApiError_ReturnsFalse()
    {
        SetupHttpResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid phone\"}");

        var result = await _service.SendMessageAsync("+34600000000", "Hola");

        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send WhatsApp message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendMessageAsync_WhenHttpThrows_ReturnsFalse()
    {
        SetupHttpException(new HttpRequestException("Connection refused"));

        var result = await _service.SendMessageAsync("+34600000000", "Hola");

        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error sending WhatsApp message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendTemplateMessageAsync_WhenApiSuccess_ReturnsTrue()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.SendTemplateMessageAsync("+34600000000", "welcome");

        result.Should().BeTrue();
    }

    [Test]
    public async Task SendTemplateMessageAsync_WithParameters_SendsCorrectPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var parameters = new Dictionary<string, string> { { "name", "Juan" } };
        await _service.SendTemplateMessageAsync("+34600000000", "welcome", parameters);

        capturedRequest.Should().NotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.GetProperty("messaging_product").GetString().Should().Be("whatsapp");
        root.GetProperty("to").GetString().Should().Be("+34600000000");
        root.GetProperty("type").GetString().Should().Be("template");
        root.GetProperty("template").GetProperty("name").GetString().Should().Be("welcome");
        root.GetProperty("template").GetProperty("language").GetProperty("code").GetString().Should().Be("es_ES");
    }

    #endregion

    #region Message format

    [Test]
    public async Task SendLineaCompletedMessageAsync_FormatsCorrectMessage()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.SendLineaCompletedMessageAsync("+34600000000", "Juan", "Pro Staff 97", "P-001");

        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp message sent successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendLineaCanceledMessageAsync_FormatsCorrectMessage()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.SendLineaCanceledMessageAsync("+34600000000", "Juan", "Pure Aero", "P-002");

        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp message sent successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task SendPedidoCanceledMessageAsync_FormatsCorrectMessage()
    {
        SetupHttpResponse(HttpStatusCode.OK);

        var result = await _service.SendPedidoCanceledMessageAsync("+34600000000", "Juan", "P-003", 3);

        result.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("WhatsApp message sent successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    #endregion

    #region Phone normalization

    [Test]
    public async Task SendMessageAsync_NormalizesPhoneNumber()
    {
        HttpRequestMessage? capturedRequest = null;
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await _service.SendMessageAsync("  +34600000000  ", "Hola");

        capturedRequest.Should().NotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("to").GetString().Should().Be("+34600000000");
    }

    #endregion
}
