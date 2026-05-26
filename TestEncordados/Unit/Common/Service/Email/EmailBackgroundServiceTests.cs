using BackEncordados.Common.Service.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Channels;

namespace TestEncordados.Unit.Common.Service.Email;

public class EmailBackgroundServiceTests
{
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<IServiceScopeFactory> _mockScopeFactory = null!;
    private Mock<IServiceScope> _mockScope = null!;
    private Mock<IServiceProvider> _mockScopedServiceProvider = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<ILogger<EmailBackgroundService>> _mockLogger = null!;
    private Channel<EmailMessage> _channel = null!;

    [SetUp]
    public void SetUp()
    {
        _channel = Channel.CreateUnbounded<EmailMessage>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopedServiceProvider = new Mock<IServiceProvider>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<EmailBackgroundService>>();

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);

        _mockScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(_mockScope.Object);

        _mockScope
            .Setup(x => x.ServiceProvider)
            .Returns(_mockScopedServiceProvider.Object);

        _mockScopedServiceProvider
            .Setup(x => x.GetService(typeof(IEmailService)))
            .Returns(_mockEmailService.Object);
    }

    private TestableEmailBackgroundService CreateService()
    {
        return new TestableEmailBackgroundService(
            _channel, _mockServiceProvider.Object, _mockLogger.Object);
    }

    [Test]
    public async Task ExecuteAsync_WhenChannelHasMessages_ProcessesAllMessages()
    {
        var messages = new[]
        {
            new EmailMessage { To = "a@test.com", Subject = "A", Body = "Body A" },
            new EmailMessage { To = "b@test.com", Subject = "B", Body = "Body B" }
        };

        foreach (var msg in messages)
            await _channel.Writer.WriteAsync(msg);
        _channel.Writer.Complete();

        var service = CreateService();
        await service.ExecuteAsyncPublic(default);

        _mockEmailService.Verify(x => x.SendEmailAsync(messages[0]), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync(messages[1]), Times.Once);
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Exactly(2));
    }

    [Test]
    public async Task ExecuteAsync_WhenChannelCompleteWithoutMessages_LogsStartAndStop()
    {
        _channel.Writer.Complete();

        var service = CreateService();
        await service.ExecuteAsyncPublic(default);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Servicio de email en segundo plano iniciado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Servicio de email en segundo plano detenido")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task ExecuteAsync_WhenSendEmailThrows_LogsErrorAndContinues()
    {
        var message = new EmailMessage { To = "error@test.com", Subject = "Error", Body = "Body" };
        var exception = new InvalidOperationException("SMTP connection error");
        _mockEmailService
            .Setup(x => x.SendEmailAsync(message))
            .ThrowsAsync(exception);

        await _channel.Writer.WriteAsync(message);
        _channel.Writer.Complete();

        var service = CreateService();
        await service.ExecuteAsyncPublic(default);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Error procesando email para: {message.To}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task ExecuteAsync_LogsProcessingMessage()
    {
        var message = new EmailMessage { To = "process@test.com", Subject = "Process", Body = "Body" };

        await _channel.Writer.WriteAsync(message);
        _channel.Writer.Complete();

        var service = CreateService();
        await service.ExecuteAsyncPublic(default);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Procesando email de la cola para: {message.To}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    private class TestableEmailBackgroundService : EmailBackgroundService
    {
        public TestableEmailBackgroundService(
            Channel<EmailMessage> channel,
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundService> logger
        ) : base(channel, serviceProvider, logger) { }

        public Task ExecuteAsyncPublic(CancellationToken ct) => ExecuteAsync(ct);
    }
}
