using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BackEncordados.Common.Database.Config;
using BackEncordados.Usuarios.Model;

namespace TestEncordados.Unit.Common.Database;

public class VersionInterceptorTests
{
    private static DbContextOptions<T> CreateOptions<T>(string dbName) where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(dbName + "_" + Guid.NewGuid())
            .AddInterceptors(new VersionInterceptor())
            .Options;
    }

    [Test]
    public void Added_User_VersionNotChanged()
    {
        var options = CreateOptions<VersionTestContext>(nameof(Added_User_VersionNotChanged));
        using var ctx = new VersionTestContext(options);

        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        user.Version.Should().Be(0);
    }

    [Test]
    public void Modified_User_IncrementsVersion()
    {
        var options = CreateOptions<VersionTestContext>(nameof(Modified_User_IncrementsVersion));
        using var ctx = new VersionTestContext(options);

        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        user.Username = "modified";
        ctx.SaveChanges();

        user.Version.Should().Be(1);
    }

    [Test]
    public void Modified_User_MultipleSaves_IncrementsEachTime()
    {
        var options = CreateOptions<VersionTestContext>(nameof(Modified_User_MultipleSaves_IncrementsEachTime));
        using var ctx = new VersionTestContext(options);

        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        for (var i = 1; i <= 3; i++)
        {
            user.Username = "modified" + i;
            ctx.SaveChanges();
            user.Version.Should().Be(i);
        }
    }

    [Test]
    public void NullContext_DoesNotThrow()
    {
        var interceptor = new VersionInterceptor();
        var eventData = new DbContextEventData(null, null, null);
        var result = new InterceptionResult<int>();

        var act = () => interceptor.SavingChanges(eventData, result);

        act.Should().NotThrow();
    }
}

public class VersionTestContext : DbContext
{
    public VersionTestContext(DbContextOptions<VersionTestContext> options) : base(options) { }
    public DbSet<User> Users { get; set; } = null!;
}
