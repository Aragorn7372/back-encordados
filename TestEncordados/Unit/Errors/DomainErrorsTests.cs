using BackEncordados.Common.Errors;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class DomainErrorsTests
{
    [Test]
    public void Constructor_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Custom error message";

        var error = new DomainErrors(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void Constructor_WithEmptyMessage_SetsErrorPropertyToEmptyString()
    {
        var error = new DomainErrors(string.Empty);

        error.Error.Should().BeEmpty();
    }
}