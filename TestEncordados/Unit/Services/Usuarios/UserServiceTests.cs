using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using BackEncordados.Usuarios.Service.CrudService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TestEncordados.Unit.Fixtures;
using UserServiceType = BackEncordados.Usuarios.Service.CrudService.UserService;
using IUserRepositoryType = BackEncordados.Usuarios.Repository.IUserRepository;
using ICloudinaryServiceType = BackEncordados.Common.Service.Cloudinary.ICloudinaryService;
using IEmailServiceType = BackEncordados.Common.Service.Email.IEmailService;
using ICacheServiceType = BackEncordados.Common.Service.Cache.ICacheService;

namespace TestEncordados.Unit.Services.Usuarios;

public class UserServiceTests
{
    private readonly Mock<IUserRepositoryType> _mockRepo;
    private readonly Mock<ICloudinaryServiceType> _mockCloudinary;
    private readonly Mock<IEmailServiceType> _mockEmail;
    private readonly Mock<ICacheServiceType> _mockCache;
    private readonly Mock<ILogger<UserServiceType>> _mockLogger;
    private readonly UserServiceType _service;

    public UserServiceTests()
    {
        _mockRepo = new Mock<IUserRepositoryType>();
        _mockCloudinary = CloudinaryServiceBuilder.Create();
        _mockEmail = new Mock<IEmailServiceType>();
        _mockCache = CacheServiceBuilder.Create();
        _mockLogger = new Mock<ILogger<UserServiceType>>();
        _service = new UserServiceType(
            _mockLogger.Object,
            _mockRepo.Object,
            _mockCloudinary.Object,
            _mockEmail.Object,
            _mockCache.Object);
    }

    private static FilterUserDto CreateFilter(string search = "") =>
        new(FindUsers: true, FindEncorders: null, FindSupervisors: null, TournamentId: null, Search: search, Page: 0, Size: 10, SortBy: "name", Direction: "asc");

    [Test]
    public async Task FindByIdAsync_ExistingUser_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);

        var result = await _service.FindByIdAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Username.Should().Be(user.Username);
    }

    [Test]
    public async Task FindByIdAsync_NonExistingUser_ReturnsNotFoundError()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _service.FindByIdAsync(userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task DeleteUserAsync_ExistingUser_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.DeleteAsync(userId)).Returns(Task.CompletedTask);

        var result = await _service.DeleteUserAsync(userId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteUserAsync_NonExistingUser_ReturnsNotFoundError()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _service.DeleteUserAsync(userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task GiveRoleToUserAsync_ExistingUser_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(UserBuilder.StandardUser());
        _mockRepo.Setup(r => r.UserChageRoleAsync(userId, User.UserRoles.ADMIN)).ReturnsAsync(true);

        var result = await _service.GiveRoleToUserAsync(userId, User.UserRoles.ADMIN);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Test]
    public async Task GiveRoleToUserAsync_NonExistingUser_ReturnsFailure()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.UserChageRoleAsync(userId, It.IsAny<string>())).ReturnsAsync(false);

        var result = await _service.GiveRoleToUserAsync(userId, User.UserRoles.ADMIN);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetAllUsersAsync_ReturnsPagedResponse()
    {
        var filter = CreateFilter();
        var users = new List<User> { UserBuilder.StandardUser() };
        var pagedResult = (Items: (IEnumerable<User>)users, TotalCount: 1);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync(pagedResult);

        var result = await _service.GetAllUsersAsync(filter);

        result.Content.Should().HaveCount(1);
        result.TotalElements.Should().Be(1);
    }

    [Test]
    public async Task GetAllUsersAsync_EmptyResult_ReturnsEmptyPage()
    {
        var filter = CreateFilter();
        var pagedResult = (Items: (IEnumerable<User>)new List<User>(), TotalCount: 0);

        _mockRepo.Setup(r => r.FindAllAsync(filter)).ReturnsAsync(pagedResult);

        var result = await _service.GetAllUsersAsync(filter);

        result.Content.Should().BeEmpty();
        result.TotalElements.Should().Be(0);
    }

    [Test]
    public async Task PatchUserAsync_ExistingUser_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);
        var dto = new UserRequestDto { Name = "Updated Name" };

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.PatchUserAsync(userId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task PatchUserAsync_NonExistingUser_ReturnsNotFoundError()
    {
        var userId = Ulid.NewUlid();
        var dto = new UserRequestDto { Name = "Updated Name" };

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _service.PatchUserAsync(userId, dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task PatchUserAsync_WithUsername_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);
        var dto = new UserRequestDto { Username = "newname" };

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.PatchUserAsync(userId, dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateContacto_ValidDto_ReturnsSuccess()
    {
        var dto = new ContactoPostRequestDto
        {
            Name = "Test Contact",
            Email = "contact@test.com",
            Phone = "1234567890"
        };
        var user = UserBuilder.Create();

        _mockRepo.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(user);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Ulid>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);
        _mockEmail.Setup(e => e.EnqueueEmailAsync(It.IsAny<EmailMessage>())).Returns(Task.CompletedTask);

        var result = await _service.CreateContacto(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateContacto_WithoutEmail_ReturnsSuccess()
    {
        var dto = new ContactoPostRequestDto
        {
            Name = "Test Contact",
            Phone = "1234567890"
        };
        var user = UserBuilder.Create();

        _mockRepo.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.CreateContacto(dto);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateEncoderAsync_ExistingUser_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.CreateEncoderAsync(userId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateEncoderAsync_NonExistingUser_ReturnsNotFoundError()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _service.CreateEncoderAsync(userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }

    [Test]
    public async Task CreateEncoderAsync_AlreadyEncorder_ReturnsConflictError()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.EncorderUser(userId);

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);

        var result = await _service.CreateEncoderAsync(userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    [Test]
    public async Task AddBonosAsync_ValidCantidad_ReturnsSuccess()
    {
        var userId = Ulid.NewUlid();
        var user = UserBuilder.StandardUser(userId);

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _service.AddBonosAsync(userId, 100.0);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AddBonosAsync_ZeroCantidad_ReturnsValidationError()
    {
        var userId = Ulid.NewUlid();

        var result = await _service.AddBonosAsync(userId, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task AddBonosAsync_NegativeCantidad_ReturnsValidationError()
    {
        var userId = Ulid.NewUlid();

        var result = await _service.AddBonosAsync(userId, -50.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task AddBonosAsync_NonExistingUser_ReturnsNotFoundError()
    {
        var userId = Ulid.NewUlid();

        _mockRepo.Setup(r => r.FindByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _service.AddBonosAsync(userId, 100.0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UserNotFoundError>();
    }
}