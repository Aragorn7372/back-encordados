using BackEncordados.Common.Service.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Common.Service.Email;

public class EmailMessageTests
{
    [Test]
    public void DefaultConstructor_SetsDefaultValues()
    {
        var message = new EmailMessage();

        message.To.Should().Be(string.Empty);
        message.Subject.Should().Be(string.Empty);
        message.Body.Should().Be(string.Empty);
        message.IsHtml.Should().BeTrue();
    }

    [Test]
    public void Constructor_SetsAllProperties()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "<p>Test body</p>",
            IsHtml = true
        };

        message.To.Should().Be("test@example.com");
        message.Subject.Should().Be("Test Subject");
        message.Body.Should().Be("<p>Test body</p>");
        message.IsHtml.Should().BeTrue();
    }

    [Test]
    public void CanModifyProperties()
    {
        var message = new EmailMessage();

        message.To = "recipient@example.com";
        message.Subject = "Subject";
        message.Body = "Body";
        message.IsHtml = false;

        message.To.Should().Be("recipient@example.com");
        message.Subject.Should().Be("Subject");
        message.Body.Should().Be("Body");
        message.IsHtml.Should().BeFalse();
    }

    [Test]
    public void IsHtml_DefaultIsTrue()
    {
        var message = new EmailMessage();

        message.IsHtml.Should().BeTrue();
    }

    [Test]
    public void IsHtml_CanBeSetToFalse()
    {
        var message = new EmailMessage { IsHtml = false };

        message.IsHtml.Should().BeFalse();
    }
}

public class MemoryEmailServiceTests
{
    private Mock<ILogger<MemoryEmailService>> _mockLogger = null!;
    private MemoryEmailService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<MemoryEmailService>>();
        _service = new MemoryEmailService(_mockLogger.Object);
    }

    [Test]
    public async Task SendEmailAsync_CompletesSuccessfully()
    {
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "<p>Test Body</p>",
            IsHtml = true
        };

        var act = () => _service.SendEmailAsync(message);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SendEmailAsync_ReturnsCompletedTask()
    {
        var message = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Subject",
            Body = "Body"
        };

        await _service.SendEmailAsync(message);
    }

    [Test]
    public async Task EnqueueEmailAsync_CompletesSuccessfully()
    {
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var act = () => _service.EnqueueEmailAsync(message);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task EnqueueEmailAsync_ReturnsCompletedTask()
    {
        var message = new EmailMessage
        {
            To = "test@test.com",
            Subject = "Subject",
            Body = "Body"
        };

        await _service.EnqueueEmailAsync(message);
    }

    [Test]
    public async Task SendEmailAsync_CallsLogger_WithCorrectStatus()
    {
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = false
        };

        await _service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("=== EMAIL SENT ===")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task EnqueueEmailAsync_CallsLogger_WithCorrectStatus()
    {
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        await _service.EnqueueEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("=== EMAIL ENQUEUED ===")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_LogsRecipient()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            Body = "Body"
        };

        await _service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Para: test@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_LogsSubject()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Body"
        };

        await _service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Asunto: Test Subject")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_LogsHtmlType_WhenIsHtml()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            Body = "<p>HTML</p>",
            IsHtml = true
        };

        await _service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Tipo: HTML")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task SendEmailAsync_LogsTextType_WhenIsNotHtml()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            Body = "Plain text",
            IsHtml = false
        };

        await _service.SendEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Tipo: Texto plano")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task EnqueueEmailAsync_LogsRecipient()
    {
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            Body = "Body"
        };

        await _service.EnqueueEmailAsync(message);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Para: test@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}