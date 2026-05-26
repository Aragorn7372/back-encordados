using BackEncordados.Purchased.Dto;
using BackEncordados.Purchased.Validator;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Purchased;

public class PurchasedPatchValidatorTests
{
    private PurchasedPatchValidator CreateValidator() => new();

    private static PurchasedPatchDto ValidDto() => new()
    {
        Machine = "Machine-1",
        Comments = "Test comment"
    };

    private static PurchasedPatchDto DtoWithMachine(string? machine) => new()
    {
        Machine = machine
    };

    private static PurchasedPatchDto DtoWithComments(string? comments) => new()
    {
        Comments = comments
    };

    [Test]
    public void Validate_AllOptionalFieldsNull_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = new PurchasedPatchDto();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_MachineAtMinLength_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithMachine("A");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(PurchasedPatchDto.Machine));
    }

    [Test]
    public void Validate_MachineAtMaxLength_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithMachine(new string('a', 100));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(PurchasedPatchDto.Machine));
    }

    [Test]
    public void Validate_MachineEmpty_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithMachine(null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(PurchasedPatchDto.Machine));
    }

    [Test]
    public void Validate_MachineExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithMachine(new string('a', 101));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchasedPatchDto.Machine) &&
            e.ErrorMessage.Contains("100"));
    }

    [Test]
    public void Validate_CommentsAtMaxLength_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithComments(new string('a', 500));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(PurchasedPatchDto.Comments));
    }

    [Test]
    public void Validate_CommentsEmpty_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = DtoWithComments(null);

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(PurchasedPatchDto.Comments));
    }

    [Test]
    public void Validate_CommentsExceedsMaxLength_ReturnsError()
    {
        var validator = CreateValidator();
        var dto = DtoWithComments(new string('a', 501));

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PurchasedPatchDto.Comments) &&
            e.ErrorMessage.Contains("500"));
    }

    [Test]
    public void Validate_MultipleFieldsAtOnce_ReturnsCorrectErrors()
    {
        var validator = CreateValidator();
        var dto = new PurchasedPatchDto
        {
            Machine = new string('a', 101),
            Comments = new string('a', 501)
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PurchasedPatchDto.Machine));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(PurchasedPatchDto.Comments));
    }

    [Test]
    public void Validate_ValidMachineAndComments_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var dto = ValidDto();

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}