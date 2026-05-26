using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BackEncordados.Common.Database.Helpers;
using BackEncordados.Usuarios.Model;

namespace TestEncordados.Unit.Common.Database;

public class TimestampInterceptorTests
{
    private static DbContextOptions<T> CreateOptions<T>(string dbName) where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(dbName + "_" + Guid.NewGuid())
            .AddInterceptors(new TimestampInterceptor())
            .Options;
    }

    [Test]
    public void Added_Entity_SetsCreatedAtAndUpdatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var options = CreateOptions<TimestampTestContext>(nameof(Added_Entity_SetsCreatedAtAndUpdatedAt));
        using var ctx = new TimestampTestContext(options);

        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        user.CreatedAt.Should().BeAfter(before);
        user.UpdatedAt.Should().BeAfter(before);
        user.CreatedAt.Should().BeCloseTo(user.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Modified_Entity_UpdatesOnlyUpdatedAt()
    {
        var options = CreateOptions<TimestampTestContext>(nameof(Modified_Entity_UpdatesOnlyUpdatedAt));
        using var ctx = new TimestampTestContext(options);

        var user = new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        var createdAt = user.CreatedAt;
        Thread.Sleep(10);

        user.Username = "modified";
        ctx.SaveChanges();

        user.UpdatedAt.Should().BeAfter(createdAt);
        user.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    public void NullContext_DoesNotThrow()
    {
        var interceptor = new TimestampInterceptor();
        var eventData = new DbContextEventData(null, null, null);
        var result = new InterceptionResult<int>();

        var act = () => interceptor.SavingChanges(eventData, result);

        act.Should().NotThrow();
    }

    [Test]
    public void NonITimestamped_Entity_IsNotAffected()
    {
        var options = new DbContextOptionsBuilder<NonTimestampedContext>()
            .UseInMemoryDatabase(nameof(NonITimestamped_Entity_IsNotAffected) + "_" + Guid.NewGuid())
            .AddInterceptors(new TimestampInterceptor())
            .Options;

        using var ctx = new NonTimestampedContext(options);

        var entity = new NonTimestampedEntity { Name = "original" };
        ctx.Entities.Add(entity);
        ctx.SaveChanges();

        var firstSave = entity.Name;

        entity.Name = "modified";
        ctx.SaveChanges();

        entity.Name.Should().Be("modified");
    }
}

public class TimestampTestContext : DbContext
{
    public TimestampTestContext(DbContextOptions<TimestampTestContext> options) : base(options) { }
    public DbSet<User> Users { get; set; } = null!;
}

public class NonTimestampedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NonTimestampedContext : DbContext
{
    public NonTimestampedContext(DbContextOptions<NonTimestampedContext> options) : base(options) { }
    public DbSet<NonTimestampedEntity> Entities { get; set; } = null!;
}
