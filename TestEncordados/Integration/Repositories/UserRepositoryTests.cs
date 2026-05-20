using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using FilterUserDto = BackEncordados.Usuarios.Dto.FilterUserDto;

namespace TestEncordados.Integration.Repositories;

public class UserRepositoryTests
{
    private PostgreSqlContainer _postgres = null!;
    private UserDbContext _context = null!;
    private UserRepository _repository = null!;
    private Mock<ILogger<UserRepository>> _loggerMock = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("users_test_isolated")
            .Build();
        
        await _postgres.StartAsync();
        
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        _context = new UserDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        
        _loggerMock = new Mock<ILogger<UserRepository>>();
        _repository = new UserRepository(_context, _loggerMock.Object);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task SaveAsync_ValidUser_ReturnsUserWithId()
    {
        var user = new User
        {
            Username = $"test_user_{Ulid.NewUlid()}",
            Email = $"test_{Ulid.NewUlid()}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };

        var result = await _repository.SaveAsync(user);

        result.Id.Should().NotBe(default);
        result.Username.Should().StartWith("test_user_");
    }

    [Test]
    public async Task FindByIdAsync_ExistingUser_ReturnsUser()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"find_test_{unique}",
            Email = $"find_{unique}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);

        var result = await _repository.FindByIdAsync(savedUser.Id);

        result.Should().NotBeNull();
        result!.Username.Should().StartWith("find_test_");
    }

    [Test]
    public async Task FindByIdAsync_NonExistingUser_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync(Ulid.NewUlid());

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByUsernameAsync_ExistingUser_ReturnsUser()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"username_test_{unique}",
            Email = $"user_{unique}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        await _repository.SaveAsync(user);

        var result = await _repository.FindByUsernameAsync($"username_test_{unique}");

        result.Should().NotBeNull();
        result!.Username.Should().Be($"username_test_{unique}");
    }

    [Test]
    public async Task FindByUsernameAsync_NonExistingUser_ReturnsNull()
    {
        var result = await _repository.FindByUsernameAsync("non_existing_" + Ulid.NewUlid());

        result.Should().BeNull();
    }

    [Test]
    public async Task FindByEmailAsync_ExistingUser_ReturnsUser()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"email_test_{unique}",
            Email = $"emailtest_{unique}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        await _repository.SaveAsync(user);

        var result = await _repository.FindByEmailAsync($"emailtest_{unique}@test.com");

        result.Should().NotBeNull();
        result!.Email.Should().Contain(unique);
    }

    [Test]
    public async Task UpdateAsync_ExistingUser_UpdatesUser()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"update_test_{unique}",
            Email = $"update_{unique}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);
        
        savedUser.Name = "Updated Name";
        savedUser.Email = $"updated_{unique}@test.com";

        var result = await _repository.UpdateAsync(savedUser);

        result.Name.Should().Be("Updated Name");
        result.Email.Should().Contain("updated_");
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_MarksAsDeleted()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"delete_test_{unique}",
            Email = $"delete_{unique}@test.com",
            PasswordHash = "hash123",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);
        var userId = savedUser.Id;

        await _repository.DeleteAsync(userId);

        var result = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task FindAllAsync_WithSearch_FiltersByUsername()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"searchable_user_{unique}",
            Email = $"search_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"other_user_{unique}",
            Email = $"other_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            null,
            null,
            $"searchable_user_{unique}",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Username.Should().Be($"searchable_user_{unique}");
    }

    [Test]
    public async Task FindAllAsync_WithRoleFilter_FiltersByRole()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"regular_user_{unique}",
            Email = $"regular_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"admin_user_{unique}",
            Email = $"admin_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.ADMIN,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            true,
            null,
            null,
            null,
            $"regular_user_{unique}",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Role.Should().Be(User.UserRoles.USER);
    }

    [Test]
    public async Task UserChageRoleAsync_ExistingUser_ChangesRole()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"role_test_{unique}",
            Email = $"role_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);

        var result = await _repository.UserChageRoleAsync(savedUser.Id, User.UserRoles.ADMIN);

        result.Should().BeTrue();
        
        var updatedUser = await _repository.FindByIdAsync(savedUser.Id);
        updatedUser!.Role.Should().Be(User.UserRoles.ADMIN);
    }

    [Test]
    public async Task UserChageRoleAsync_NonExistingUser_ReturnsFalse()
    {
        var result = await _repository.UserChageRoleAsync(Ulid.NewUlid(), User.UserRoles.ADMIN);

        result.Should().BeFalse();
    }

    [Test]
    public async Task GetActiveUsersAsync_ReturnsOnlyNonDeletedUsers()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"active1_{unique}",
            Email = $"active1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"active2_{unique}",
            Email = $"active2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"todelete_{unique}",
            Email = $"todelete_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = true
        });

        var result = await _repository.GetActiveUsersAsync();

        result.Should().Contain(u => u.Username == $"active1_{unique}");
        result.Should().Contain(u => u.Username == $"active2_{unique}");
        result.Should().NotContain(u => u.Username == $"todelete_{unique}");
    }

    [Test]
    public async Task FindByIdsAsync_ExistingIds_ReturnsUsers()
    {
        var unique = Ulid.NewUlid().ToString();
        var user1 = await _repository.SaveAsync(new User
        {
            Username = $"ids_user1_{unique}",
            Email = $"ids1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        var user2 = await _repository.SaveAsync(new User
        {
            Username = $"ids_user2_{unique}",
            Email = $"ids2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var result = await _repository.FindByIdsAsync(new[] { user1.Id, user2.Id });

        result.Count().Should().Be(2);
    }

    [Test]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        var result = await _repository.FindByIdsAsync(Array.Empty<Ulid>());

        result.Should().BeEmpty();
    }
}