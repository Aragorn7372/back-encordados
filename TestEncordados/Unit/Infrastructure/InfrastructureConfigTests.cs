using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.Memory;
using BackEncordados.Common.Service.Email;
using BackEncordados.Infraestructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Infrastructure;

public class InfrastructureConfigTests
{
    [Test]
    public void AddRateLimitingPolicy_ConfiguresAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRateLimitingPolicy();

        using var provider = services.BuildServiceProvider();
        var counterStore = provider.GetService<AspNetCoreRateLimit.IRateLimitCounterStore>();
        var rateLimitConfig = provider.GetService<AspNetCoreRateLimit.IRateLimitConfiguration>();

        counterStore.Should().NotBeNull();
        rateLimitConfig.Should().NotBeNull();
    }

    [Test]
    public void AddCache_InProduction_RegistersHybridCacheService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Development"] = "false",
                ["redis:url"] = "localhost:6379"
            })
            .Build();

        services.AddCache(config);

        using var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<BackEncordados.Common.Service.Cache.Hybrid.HybridCacheService>();
    }

    [Test]
    public void AddCache_WhenDevelopmentMissing_DefaultsToDevelopment()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCache(config);

        using var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<MemoryCacheService>();
    }

    [Test]
    public void AddCache_InProductionWithoutRedisUrl_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Development"] = "false"
            })
            .Build();

        services.AddCache(config);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<ICacheService>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Redis cache URL no configurada*");
    }

    [Test]
    public void AddDatabase_RegistersAllDbContexts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Development"] = "true"
            })
            .Build();

        services.AddDatabase(config);

        using var provider = services.BuildServiceProvider();
        var userCtx = provider.GetService<UserDbContext>();
        var materialsCtx = provider.GetService<MaterialsDbContext>();
        var pedidosCtx = provider.GetService<PedidosDbContext>();
        var talleresCtx = provider.GetService<TalleresDbContext>();

        userCtx.Should().NotBeNull();
        materialsCtx.Should().NotBeNull();
        pedidosCtx.Should().NotBeNull();
        talleresCtx.Should().NotBeNull();
    }

    [Test]
    public void AddDatabase_CanQueryInMemory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Development"] = "true"
            })
            .Build();

        services.AddDatabase(config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        ctx.Database.EnsureCreated();
        ctx.Users.Should().NotBeNull();
    }

    [Test]
    public void AddEmail_InDevelopment_RegistersMemoryEmailService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        services.AddEmail(envMock.Object);

        using var provider = services.BuildServiceProvider();
        var emailService = provider.GetService<IEmailService>();
        emailService.Should().NotBeNull();
        emailService.Should().BeOfType<MemoryEmailService>();
    }

    [Test]
    public void AddEmail_InProduction_RegistersMailKitEmailService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        services.AddEmail(envMock.Object);

        using var provider = services.BuildServiceProvider();
        var emailService = provider.GetService<IEmailService>();
        emailService.Should().NotBeNull();
        emailService.Should().BeOfType<MailKitEmailService>();
    }

    [Test]
    public void AddEmail_InProduction_RegistersBackgroundService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        services.AddEmail(envMock.Object);

        using var provider = services.BuildServiceProvider();
        var bgService = provider.GetService<Microsoft.Extensions.Hosting.IHostedService>();
        bgService.Should().NotBeNull();
        bgService.Should().BeOfType<EmailBackgroundService>();
    }
}
