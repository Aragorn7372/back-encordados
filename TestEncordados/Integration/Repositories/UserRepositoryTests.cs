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

    [TearDown]
    public async Task TearDown()
    {
        // Clean up after each test to ensure isolation
        var users = _context.Users.IgnoreQueryFilters().ToList();
        foreach (var user in users)
        {
            _context.Users.Remove(user);
        }
        await _context.SaveChangesAsync();
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

    [Test]
    public async Task FindAllAsync_WithTournamentId_FiltersByTournament()
    {
        var unique = Ulid.NewUlid().ToString();
        var tournamentId = Ulid.NewUlid();
        await _repository.SaveAsync(new User
        {
            Username = $"tour_user1_{unique}",
            Email = $"tour1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            TournamentId = tournamentId,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"tour_user2_{unique}",
            Email = $"tour2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            TournamentId = Ulid.NewUlid(),
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            null,
            tournamentId,
            "",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Username.Should().Be($"tour_user1_{unique}");
    }

    [Test]
    public async Task FindAllAsync_FindEncordersTrue_FiltersEncorders()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"enc_user1_{unique}",
            Email = $"enc1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.ENCORDER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"enc_user2_{unique}",
            Email = $"enc2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            true,
            null,
            null,
            $"enc_user",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        items.Should().ContainSingle();
        items.First().Role.Should().Be(User.UserRoles.ENCORDER);
    }

    [Test]
    public async Task FindAllAsync_FindSupervisorsTrue_FiltersSupervisors()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"sup_user1_{unique}",
            Email = $"sup1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.SUPERVISOR,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"sup_user2_{unique}",
            Email = $"sup2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            true,
            null,
            $"sup_user",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        items.Should().ContainSingle();
        items.First().Role.Should().Be(User.UserRoles.SUPERVISOR);
    }

    [Test]
    public async Task FindAllAsync_WithSearchLikeName_FiltersByName()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"name_user1_{unique}",
            Name = $"SpecialName_{unique}",
            Email = $"name1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });
        await _repository.SaveAsync(new User
        {
            Username = $"name_user2_{unique}",
            Name = $"NormalName_{unique}",
            Email = $"name2_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            null,
            null,
            $"SpecialName_{unique}",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Name.Should().Be($"SpecialName_{unique}");
    }

    [Test]
    public async Task FindAllAsync_WithSearchLikeEmail_FiltersByEmail()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"email_user1_{unique}",
            Email = $"target_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            null,
            null,
            $"target_{unique}",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Email.Should().Be($"target_{unique}@test.com");
    }

    [Test]
    public async Task FindAllAsync_WithSearchLikePhone_FiltersByPhone()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User
        {
            Username = $"phone_user1_{unique}",
            Phone = $"+34999{unique[..6]}",
            Email = $"phone1_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var filter = new FilterUserDto(
            null,
            null,
            null,
            null,
            $"+34999{unique[..6]}",
            0,
            10,
            "id",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(1);
        items.First().Phone.Should().Be($"+34999{unique[..6]}");
    }

    [Test]
    public async Task FindAllAsync_Pagination_ReturnsCorrectSlice()
    {
        var unique = Ulid.NewUlid().ToString();
        for (int i = 0; i < 5; i++)
        {
            await _repository.SaveAsync(new User
            {
                Username = $"pag_{i}_{unique}",
                Email = $"pag_{i}_{unique}@test.com",
                PasswordHash = "hash",
                Role = User.UserRoles.USER,
                IsDeleted = false
            });
        }

        var filter = new FilterUserDto(
            null,
            null,
            null,
            null,
            $"pag_",
            1,
            2,
            "username",
            "asc");
        var (items, totalCount) = await _repository.FindAllAsync(filter);

        totalCount.Should().Be(5);
        items.Count().Should().Be(2);
        items.Select(u => u.Username).Should().ContainInOrder($"pag_2_{unique}", $"pag_3_{unique}");
    }

    [Test]
    public async Task FindAllAsync_SortByNameDesc_ReturnsSortedItems()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User { Username = $"sort_name_a_{unique}", Name = $"A_name_{unique}", Email = $"sorta_{unique}@test.com", PasswordHash = "hash" });
        await _repository.SaveAsync(new User { Username = $"sort_name_b_{unique}", Name = $"B_name_{unique}", Email = $"sortb_{unique}@test.com", PasswordHash = "hash" });

        var filter = new FilterUserDto(null, null, null, null, $"sort_name_", 0, 10, "name", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.Select(u => u.Name).Should().ContainInOrder($"B_name_{unique}", $"A_name_{unique}");
    }

    [Test]
    public async Task FindAllAsync_SortByUsernameAsc_ReturnsSortedItems()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User { Username = $"sort_user_b_{unique}", Email = $"sortb_{unique}@test.com", PasswordHash = "hash" });
        await _repository.SaveAsync(new User { Username = $"sort_user_a_{unique}", Email = $"sorta_{unique}@test.com", PasswordHash = "hash" });

        var filter = new FilterUserDto(null, null, null, null, $"sort_user_", 0, 10, "username", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.Select(u => u.Username).Should().ContainInOrder($"sort_user_a_{unique}", $"sort_user_b_{unique}");
    }

    [Test]
    public async Task FindAllAsync_SortByEmailDesc_ReturnsSortedItems()
    {
        var unique = Ulid.NewUlid().ToString();
        await _repository.SaveAsync(new User { Username = $"sort_email_a_{unique}", Email = $"sorta_{unique}@test.com", PasswordHash = "hash" });
        await _repository.SaveAsync(new User { Username = $"sort_email_b_{unique}", Email = $"sortb_{unique}@test.com", PasswordHash = "hash" });

        var filter = new FilterUserDto(null, null, null, null, $"sort_email_", 0, 10, "email", "desc");
        var (items, _) = await _repository.FindAllAsync(filter);

        items.Select(u => u.Email).Should().ContainInOrder($"sortb_{unique}@test.com", $"sorta_{unique}@test.com");
    }

     [Test]
    public async Task FindAllAsync_SortByCreatedAtAsc_ReturnsSortedItems()
    {
        var unique = Ulid.NewUlid().ToString();
        // Create users with different creation times separated by at least 1 second
        var user1 = new User { Username = $"sort_cat_1_{unique}", Email = $"sortcat1_{unique}@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow.AddSeconds(-2) };
        await _repository.SaveAsync(user1);
        
        // Add small delay to ensure different timestamp
        await Task.Delay(100);
        
        var user2 = new User { Username = $"sort_cat_2_{unique}", Email = $"sortcat2_{unique}@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        await _repository.SaveAsync(user2);

        var filter = new FilterUserDto(null, null, null, null, $"sort_cat_", 0, 10, "createdAt", "asc");
        var (items, _) = await _repository.FindAllAsync(filter);

        // Verify items are sorted by CreatedAt in ascending order
        var sortedItems = items.ToList();
        sortedItems.Count.Should().Be(2);
        var isSorted = sortedItems.SequenceEqual(sortedItems.OrderBy(u => u.CreatedAt));
        isSorted.Should().BeTrue("CreatedAt values should be in ascending order");
    }

    [Test]
    public async Task UserChageRoleAsync_SameRole_ReturnsFalseAndDoesNotSave()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"role_same_{unique}",
            Email = $"rolesame_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);

        var result = await _repository.UserChageRoleAsync(savedUser.Id, User.UserRoles.USER);

        result.Should().BeFalse();
    }

    [Test]
    public async Task DeleteAsync_ExistingUser_ReplacesUsernameWithUuidPrefix()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = new User
        {
            Username = $"del_prefix_{unique}",
            Email = $"del_prefix_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        };
        var savedUser = await _repository.SaveAsync(user);

        await _repository.DeleteAsync(savedUser.Id);

        var deletedUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == savedUser.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
        deletedUser.Username.Should().StartWith("deleted_");
    }

    [Test]
    public async Task DeleteAsync_NonExistingUser_DoesNothing()
    {
        var nonExistentId = Ulid.NewUlid();
        
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId);
        
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task FindByIdsAsync_SomeExistingSomeNonExisting_ReturnsOnlyExisting()
    {
        var unique = Ulid.NewUlid().ToString();
        var user = await _repository.SaveAsync(new User
        {
            Username = $"partial_ids_{unique}",
            Email = $"partial_{unique}@test.com",
            PasswordHash = "hash",
            Role = User.UserRoles.USER,
            IsDeleted = false
        });

        var result = await _repository.FindByIdsAsync(new[] { user.Id, Ulid.NewUlid() });

        result.Count().Should().Be(1);
        result.First().Id.Should().Be(user.Id);
    }
}