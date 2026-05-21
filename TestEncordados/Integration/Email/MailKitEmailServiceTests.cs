using System.Text.Json;
using BackEncordados.Common.Service.Email;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Channels;

namespace TestEncordados.Integration.Email;

[TestFixture]
public class MailKitEmailServiceTests
{
    private IContainer _mailHogContainer = null!;
    private string _smtpHost = null!;
    private int _smtpPort;
    private int _apiPort;
    private HttpClient _httpClient = null!;

    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<ILogger<MailKitEmailService>> _mockLogger = null!;
    private Channel<EmailMessage> _channel = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _mailHogContainer = new ContainerBuilder()
            .WithImage("mailhog/mailhog")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .Build();

        await _mailHogContainer.StartAsync();

        _smtpHost = _mailHogContainer.Hostname;
        _smtpPort = _mailHogContainer.GetMappedPublicPort(1025);
        _apiPort = _mailHogContainer.GetMappedPublicPort(8025);
        _httpClient = new HttpClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _httpClient.Dispose();
        if (_mailHogContainer != null)
        {
            await _mailHogContainer.StopAsync();
            await _mailHogContainer.DisposeAsync();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _channel = Channel.CreateUnbounded<EmailMessage>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<MailKitEmailService>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            await _httpClient.DeleteAsync($"http://localhost:{_apiPort}/api/v1/messages");
        }
        catch
        {
            // ignore cleanup errors
        }
    }

    private MailKitEmailService CreateService()
    {
        return new MailKitEmailService(
            _mockConfig.Object,
            _mockLogger.Object,
            _channel
        );
    }

    private MailKitEmailService CreateServiceWithSmtpSettings(
        string? host = null,
        string? port = null,
        string? username = null,
        string? password = null,
        string? fromEmail = null,
        string? fromName = null)
    {
        _mockConfig.Setup(x => x["Smtp:Host"]).Returns(host ?? _smtpHost);
        _mockConfig.Setup(x => x["Smtp:Port"]).Returns(port ?? _smtpPort.ToString());
        _mockConfig.Setup(x => x["Smtp:Username"]).Returns(username ?? "testuser");
        _mockConfig.Setup(x => x["Smtp:Password"]).Returns(password);
        _mockConfig.Setup(x => x["Smtp:FromEmail"]).Returns(fromEmail ?? "sender@test.com");
        _mockConfig.Setup(x => x["Smtp:FromName"]).Returns(fromName ?? "Test Sender");
        return CreateService();
    }

    [Test]
    public async Task SendEmailAsync_WhenSmtpConfigured_SendsEmailSuccessfully()
    {
        var message = new EmailMessage
        {
            To = "recipient@test.com",
            Subject = "Test Subject",
            Body = "Hello from Testcontainers!",
            IsHtml = false
        };

        var service = CreateServiceWithSmtpSettings();
        await service.SendEmailAsync(message);

        await Task.Delay(500);

        var response = await _httpClient.GetAsync($"http://localhost:{_apiPort}/api/v2/messages");
        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");

        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var sent = items[0];
        var headers = sent.GetProperty("Content").GetProperty("Headers");

        headers.GetProperty("To")[0].GetString()!.Should().Contain("recipient@test.com");
        headers.GetProperty("Subject")[0].GetString().Should().Be("Test Subject");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Email enviado exitosamente a: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_WhenSmtpNotConfigured_LogsWarning()
    {
        _mockConfig.Setup(x => x["Smtp:Host"]).Returns((string?)null);
        _mockConfig.Setup(x => x["Smtp:Port"]).Returns("587");
        _mockConfig.Setup(x => x["Smtp:Username"]).Returns((string?)null);

        var message = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Body"
        };

        var service = CreateService();
        await service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("SMTP no configurado, omitiendo envío de email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_WhenPortIsInvalid_ThrowsAndLogsError()
    {
        var service = CreateServiceWithSmtpSettings(port: "invalid_port");

        var message = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Body"
        };

        var act = async () => await service.SendEmailAsync(message);

        await act.Should().ThrowAsync<FormatException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al enviar email a: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_WhenSmtpUnreachable_LogsErrorAndRethrows()
    {
        var service = CreateServiceWithSmtpSettings(host: "localhost", port: "1");

        var message = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Test",
            Body = "Body"
        };

        var act = async () => await service.SendEmailAsync(message);

        await act.Should().ThrowAsync<Exception>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al enviar email a: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task EnqueueEmailAsync_WhenChannelWritable_EnqueuesMessage()
    {
        var message = new EmailMessage
        {
            To = "enqueue@test.com",
            Subject = "Enqueue Test",
            Body = "Enqueue Body"
        };

        var service = CreateService();
        await service.EnqueueEmailAsync(message);

        var reader = await _channel.Reader.ReadAsync();
        reader.Should().BeSameAs(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Email encolado para procesamiento en segundo plano a: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task EnqueueEmailAsync_WhenChannelClosed_LogsError()
    {
        _channel.Writer.Complete();

        var message = new EmailMessage
        {
            To = "error@test.com",
            Subject = "Error Test",
            Body = "Error Body"
        };

        var service = CreateService();
        await service.EnqueueEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error al encolar email para: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}
