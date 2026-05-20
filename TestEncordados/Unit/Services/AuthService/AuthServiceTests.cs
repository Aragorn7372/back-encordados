using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AuthServiceType = BackEncordados.Usuarios.Service.Auth.AuthService;
using IJwtServiceType = BackEncordados.Usuarios.Service.Auth.IJwtService;

namespace TestEncordados.Unit.Services.AuthService;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IJwtServiceType> _mockJwtService;
    private readonly Mock<ILogger<AuthServiceType>> _mockLogger;
    private readonly Mock<ICacheService> _mockCache;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly AuthServiceType _service;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtServiceType>();
        _mockLogger = new Mock<ILogger<AuthServiceType>>();
        _mockCache = new Mock<ICacheService>();
        _mockEmailService = new Mock<IEmailService>();

        _service = new AuthServiceType(
            _mockUserRepo.Object,
            _mockJwtService.Object,
            _mockLogger.Object,
            _mockCache.Object,
            _mockEmailService.Object);
    }

    private static RegisterDto CreateRegisterDto(string username = "testuser", string email = "test@example.com", string password = "Password123!") =>
        new() { Username = username, Email = email, Password = password };

    private static LoginDto CreateLoginDto(string username = "testuser", string password = "Password123!") =>
        new() { Username = username, Password = password };

    private static User CreateUser(
        Ulid? id = null,
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "hashedpassword",
        string role = User.UserRoles.USER) =>
        new()
        {
            Id = id ?? Ulid.NewUlid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

    [Test]
    public async Task SignUpAsync_ValidDto_ReturnsSuccess()
    {
        var dto = CreateRegisterDto();
        var user = CreateUser();

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt_token");

        var result = await _service.SignUpAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt_token");
        result.Value.User.Username.Should().Be(dto.Username);
    }

    [Test]
    public async Task SignUpAsync_DuplicateUsername_ReturnsConflict()
    {
        var dto = CreateRegisterDto();
        var existingUser = CreateUser();

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync(existingUser);

        var result = await _service.SignUpAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task SignUpAsync_DuplicateEmail_ReturnsConflict()
    {
        var dto = CreateRegisterDto();
        var existingUser = CreateUser(email: dto.Email);

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);
        _mockUserRepo.Setup(r => r.FindByEmailAsync(dto.Email)).ReturnsAsync(existingUser);

        var result = await _service.SignUpAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task SignInAsync_ValidCredentials_ReturnsSuccess()
    {
        var dto = CreateLoginDto();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 11);
        var user = CreateUser(passwordHash: passwordHash);

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync(user);
        _mockJwtService.Setup(j => j.GenerateToken(user)).Returns("jwt_token");

        var result = await _service.SignInAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt_token");
    }

    [Test]
    public async Task SignInAsync_UserNotFound_ReturnsUnauthorized()
    {
        var dto = CreateLoginDto();

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);

        var result = await _service.SignInAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task SignInAsync_WrongPassword_ReturnsUnauthorized()
    {
        var dto = CreateLoginDto();
        var user = CreateUser(passwordHash: BCrypt.Net.BCrypt.HashPassword("wrongpassword", 11));

        _mockUserRepo.Setup(r => r.FindByUsernameAsync(dto.Username)).ReturnsAsync(user);

        var result = await _service.SignInAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Test]
    public async Task GetEmailAsync_ExistingEmail_ReturnsSuccess()
    {
        var email = "test@example.com";
        var user = CreateUser(email: email);

        _mockUserRepo.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Ulid>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockEmailService.Setup(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>()))
            .Returns(Task.CompletedTask);

        var result = await _service.GetEmailAsync(email);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetEmailAsync_NonExistingEmail_ReturnsUserNotFoundError()
    {
        var email = "nonexistent@example.com";

        _mockUserRepo.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync((User?)null);

        var result = await _service.GetEmailAsync(email);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task ChangePasswordAsync_ExpiredGuid_ReturnsTimeoutError()
    {
        var guid = Guid.NewGuid();
        var dto = new ChangePasswordRequestDto { NewPassword = "NewPassword123!" };

        _mockCache.Setup(c => c.GetAsync<Ulid?>($"PasswordChange{guid}")).ReturnsAsync((Ulid?)null);

        var result = await _service.ChangePasswordAsync(guid, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PasswordChangeExpiredTimeout>();
    }

    [Test]
    public async Task ChangePasswordAsync_ValidGuid_ReturnsSuccess()
    {
        var guid = Guid.NewGuid();
        var userId = Ulid.NewUlid();
        var dto = new ChangePasswordRequestDto { NewPassword = "NewPassword123!" };
        var user = CreateUser(id: userId, passwordHash: BCrypt.Net.BCrypt.HashPassword("oldpassword", 11));

        const string cacheKey = "password_";
        _mockCache.Setup(c => c.GetAsync<Ulid?>($"{cacheKey}{guid}")).ReturnsAsync(userId);
        _mockUserRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);
        _mockCache.Setup(c => c.RemoveAsync($"{cacheKey}{guid}")).Returns(Task.CompletedTask);

        var result = await _service.ChangePasswordAsync(guid, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ChangePasswordAsync_SamePassword_ReturnsValidationError()
    {
        var guid = Guid.NewGuid();
        var userId = Ulid.NewUlid();
        var newPassword = "SamePassword123!";
        var dto = new ChangePasswordRequestDto { NewPassword = newPassword };
        var user = CreateUser(id: userId, passwordHash: BCrypt.Net.BCrypt.HashPassword(newPassword, 11));

        const string cacheKey = "password_";
        _mockCache.Setup(c => c.GetAsync<Ulid?>($"{cacheKey}{guid}")).ReturnsAsync(userId);
        _mockUserRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);

        var result = await _service.ChangePasswordAsync(guid, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }
}