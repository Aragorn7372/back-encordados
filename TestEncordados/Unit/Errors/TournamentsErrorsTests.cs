using BackEncordados.Common.Errors;
using BackEncordados.Talleres.Error;
using FluentAssertions;

namespace TestEncordados.Unit.Errors;

public class TournamentsErrorsTests
{
    [Test]
    public void TournamentsErrors_InheritsFromDomainErrors()
    {
        var error = new TournamentsErrors("test");

        error.Should().BeAssignableTo<DomainErrors>();
    }

    [Test]
    public void TournamentsErrors_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Tournament error message";
        var error = new TournamentsErrors(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void ConflictError_InheritsFromTournamentsErrors()
    {
        var error = new ConflictError("Conflict");

        error.Should().BeAssignableTo<TournamentsErrors>();
    }

    [Test]
    public void ConflictError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Tournament already exists";
        var error = new ConflictError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }

    [Test]
    public void TournamentNotFoundError_InheritsFromTournamentsErrors()
    {
        var error = new TournamentNotFoundError();

        error.Should().BeAssignableTo<TournamentsErrors>();
    }

    [Test]
    public void TournamentNotFoundError_HasDefaultMessage()
    {
        var error = new TournamentNotFoundError();

        error.Error.Should().Be("Tournament not found");
    }

    [Test]
    public void TournamentNotFoundError_WithCustomMessage_OverridesDefault()
    {
        const string customMessage = "Tournament with id 123 not found";
        var error = new TournamentNotFoundError(customMessage);

        error.Error.Should().Be(customMessage);
    }

    [Test]
    public void ValidationError_InheritsFromTournamentsErrors()
    {
        var error = new ValidationError("Validation");

        error.Should().BeAssignableTo<TournamentsErrors>();
    }

    [Test]
    public void ValidationError_WithCustomMessage_SetsErrorPropertyCorrectly()
    {
        const string expectedMessage = "Invalid tournament data";
        var error = new ValidationError(expectedMessage);

        error.Error.Should().Be(expectedMessage);
    }
}