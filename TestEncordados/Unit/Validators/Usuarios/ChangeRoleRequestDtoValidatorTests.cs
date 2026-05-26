using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Validator;
using FluentAssertions;

namespace TestEncordados.Unit.Validators.Usuarios;

public class ChangeRoleRequestDtoValidatorTests
{
    private readonly ChangeRoleRequestDtoValidator _validator = new();

    private static ChangeRoleRequestDto CreateValidDto(string roleName) => new()
    {
        UserId = Ulid.NewUlid(),
        RoleName = roleName
    };

    private static ChangeRoleRequestDto CreateDtoWithUserId(Ulid userId) => new()
    {
        UserId = userId,
        RoleName = "ADMIN"
    };

    private static ChangeRoleRequestDto CreateDtoWithRoleName(string roleName) => new()
    {
        UserId = Ulid.NewUlid(),
        RoleName = roleName
    };

    [Test]
    public void Validate_ValidAdminRole_ReturnsNoErrors()
    {
        var dto = CreateValidDto("ADMIN");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_ValidUserRole_ReturnsNoErrors()
    {
        var dto = CreateValidDto("USER");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_ValidOwnerRole_ReturnsNoErrors()
    {
        var dto = CreateValidDto("OWNER");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_ValidEncorderRole_ReturnsNoErrors()
    {
        var dto = CreateValidDto("ENCORDER");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_RoleIsCaseInsensitive_ReturnsNoErrors()
    {
        var validRoles = new[] { "admin", "user", "owner", "encorder", "AdMiN" };

        foreach (var role in validRoles)
        {
            var dto = CreateValidDto(role);

            var result = _validator.Validate(dto);

            result.Errors.Should().NotContain(e => e.PropertyName == nameof(ChangeRoleRequestDto.RoleName));
        }
    }

    [Test]
    public void Validate_InvalidRole_ReturnsError()
    {
        var dto = CreateValidDto("SUPERADMIN");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeRoleRequestDto.RoleName));
    }

    [Test]
    public void Validate_EmptyRole_ReturnsError()
    {
        var dto = CreateDtoWithRoleName("");

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeRoleRequestDto.RoleName));
    }

    [Test]
    public void Validate_RoleNameIsNull_ReturnsError()
    {
        var dto = new ChangeRoleRequestDto
        {
            UserId = Ulid.NewUlid(),
            RoleName = null!
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeRoleRequestDto.RoleName));
    }

    [Test]
    public void Validate_ValidUserId_ReturnsNoError()
    {
        var dto = CreateDtoWithUserId(Ulid.NewUlid());

        var result = _validator.Validate(dto);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ChangeRoleRequestDto.UserId));
    }

    [Test]
    public void Validate_UserIdIsEmpty_ReturnsError()
    {
        var dto = CreateDtoWithUserId(Ulid.Empty);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeRoleRequestDto.UserId));
    }

    [Test]
    public void Validate_AllValidRoles_ReturnsNoErrors()
    {
        var validRoles = new[] { "ADMIN", "USER", "OWNER", "ENCORDER" };

        foreach (var role in validRoles)
        {
            var dto = CreateValidDto(role);

            var result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }

    [Test]
    public void Validate_InvalidRoles_ReturnsErrors()
    {
        var invalidRoles = new[] { "GUEST", "MODERATOR", "SUPERADMIN", "ROOT", "manager" };

        foreach (var role in invalidRoles)
        {
            var dto = CreateValidDto(role);

            var result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(ChangeRoleRequestDto.RoleName));
        }
    }

    [Test]
    public void Validate_ValidDto_ReturnsNoErrors()
    {
        var dto = new ChangeRoleRequestDto
        {
            UserId = Ulid.NewUlid(),
            RoleName = "ADMIN"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}