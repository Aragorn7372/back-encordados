using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Validator;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Usuarios;

public class UserRequestDtoValidatorTests
{
    private readonly UserRequestDtoValidator _validator = new();

    private static UserRequestDto CreateValidDto() => new()
    {
        Name = "Juan",
        Email = "juan@example.com",
        Telefono = "34612345678",
        Username = "juan123"
    };

    private static UserRequestDto CreateDtoWithName(string name) => new()
    {
        Name = name,
        Email = "test@example.com",
        Telefono = "34612345678",
        Username = "testuser"
    };

    private static UserRequestDto CreateDtoWithEmail(string email) => new()
    {
        Name = "Test",
        Email = email,
        Telefono = "34612345678",
        Username = "testuser"
    };

    private static UserRequestDto CreateDtoWithTelefono(string telefono) => new()
    {
        Name = "Test",
        Email = "test@example.com",
        Telefono = telefono,
        Username = "testuser"
    };

    private static UserRequestDto CreateDtoWithUsername(string username) => new()
    {
        Name = "Test",
        Email = "test@example.com",
        Telefono = "34612345678",
        Username = username
    };

    [Test]
    public void Validate_AllValidProperties_ReturnsNoErrors()
    {
        var dto = CreateValidDto();

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_NameIsNull_ReturnsNoError()
    {
        var dto = CreateDtoWithName(null!);
        dto.Name = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Name));
    }

    [Test]
    public void Validate_NameIsEmpty_ReturnsError()
    {
        var dto = CreateDtoWithName("");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Name) &&
            e.ErrorMessage.Contains("caracter"));
    }

    [Test]
    public void Validate_NameHasOneCharacter_ReturnsNoError()
    {
        var dto = CreateDtoWithName("A");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Name));
    }

    [Test]
    public void Validate_EmailIsNull_ReturnsNoError()
    {
        var dto = CreateDtoWithEmail(null!);
        dto.Email = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Email));
    }

    [Test]
    public void Validate_EmailIsValid_ReturnsNoError()
    {
        var dto = CreateDtoWithEmail("user@domain.com");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Email));
    }

    [Test]
    public void Validate_EmailWithoutAtSymbol_ReturnsError()
    {
        var dto = CreateDtoWithEmail("invalid-email.com");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Email));
    }

    [Test]
    public void Validate_EmailWithoutDomain_ReturnsError()
    {
        var dto = CreateDtoWithEmail("user@");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Email));
    }

    [Test]
    public void Validate_TelefonoIsNull_ReturnsNoError()
    {
        var dto = CreateDtoWithTelefono(null!);
        dto.Telefono = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoWithPlusPrefix_ReturnsNoError()
    {
        var dto = CreateDtoWithTelefono("+34612345678");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoWithSpaces_ReturnsNoError()
    {
        var dto = CreateDtoWithTelefono("346 123 456 78");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoWithParentheses_ReturnsNoError()
    {
        var dto = CreateDtoWithTelefono("(34) 612 345 678");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoStartsWithZero_ReturnsError()
    {
        var dto = CreateDtoWithTelefono("0123456789");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoTooShort_ReturnsError()
    {
        var dto = CreateDtoWithTelefono("123456");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    [Test]
    public void Validate_TelefonoTooLong_ReturnsError()
    {
        var dto = CreateDtoWithTelefono("12345678901234567890");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Telefono));
    }

    

    [Test]
    public void Validate_UsernameIsNull_ReturnsNoError()
    {
        var dto = CreateDtoWithUsername(null!);
        dto.Username = null;

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Username));
    }

    [Test]
    public void Validate_UsernameHasOneCharacter_ReturnsNoError()
    {
        var dto = CreateDtoWithUsername("a");

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Username));
    }

    [Test]
    public void Validate_UsernameIsEmpty_ReturnsError()
    {
        var dto = CreateDtoWithUsername("");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UserRequestDto.Username));
    }

    [Test]
    public void Validate_AllPropertiesWithValidValues_ReturnsNoErrors()
    {
        var validTelefonos = new[]
        {
            "34612345678",
            "15551234567",
            "+34612345678",
            "611223344",
            "12345678901234"
        };

        foreach (var telefono in validTelefonos)
        {
            var dto = CreateDtoWithTelefono(telefono);

            var result = _validator.Validate(dto);

            result.Errors.Should().NotContain(e => e.PropertyName == nameof(UserRequestDto.Telefono));
        }
    }
}