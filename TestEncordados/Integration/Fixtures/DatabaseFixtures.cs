using BackEncordados.Common.Database.Config;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace TestEncordados.Integration.Fixtures;

public abstract class PostgreSqlTestBase
{
    protected PostgreSqlContainer? _postgres;
    protected string ConnectionString = null!;

    [OneTimeSetUp]
    public async Task BaseOneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(GetDatabaseName())
            .Build();
        
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
        
        await InitializeDatabaseAsync();
    }

    [OneTimeTearDown]
    public async Task BaseOneTimeTearDown()
    {
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    protected abstract string GetDatabaseName();
    protected abstract Task InitializeDatabaseAsync();
}

public class UserDatabaseFixture : PostgreSqlTestBase
{
    protected override string GetDatabaseName() => "users_db";

    protected override async Task InitializeDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        using var context = new UserDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public DbContextOptions<UserDbContext> GetOptions() => 
        new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
}

public class MaterialsDatabaseFixture : PostgreSqlTestBase
{
    protected override string GetDatabaseName() => "materials_db";

    protected override async Task InitializeDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        using var context = new MaterialsDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public DbContextOptions<MaterialsDbContext> GetOptions() =>
        new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
}