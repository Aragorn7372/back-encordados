using BackEncordados.Common.Database.Config;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.Memory;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.SignalR;
using BackEncordados.Excel.Archive;
using BackEncordados.Excel.Service;
using BackEncordados.Export.Archive;
using BackEncordados.Export.Service;
using BackEncordados.Excel.Repository;
using BackEncordados.Export.Repository;
using BackEncordados.Infraestructure;
using BackEncordados.Materials.Repository.Materials;
using BackEncordados.Materials.Repository.Strings;
using BackEncordados.Materials.Service.Cuerdas;
using BackEncordados.Materials.Service.Materials;
using BackEncordados.Purchased.Repository;
using BackEncordados.Purchased.Service;
using BackEncordados.Talleres.Repository;
using BackEncordados.Talleres.Service;
using BackEncordados.Usuarios.Repository;
using BackEncordados.Usuarios.Service.Auth;
using BackEncordados.Usuarios.Service.CrudService;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Infrastructure;

public class DiRegistrationTests
{
    [Test]
    public void AddServices_RegistersAllServiceTypes()
    {
        var services = new ServiceCollection();
        services.AddServices();

        var serviceTypes = new[]
        {
            typeof(IJwtService),
            typeof(IJwtTokenExtractor),
            typeof(IAuthService),
            typeof(IUserService),
            typeof(ITournamentService),
            typeof(IPurchasedService),
            typeof(ICuerdasService),
            typeof(IMaterialsService),
            typeof(IExportArchiveManager),
            typeof(IExportService),
            typeof(IExcelArchiveManager),
            typeof(IExcelService),
        };

        foreach (var type in serviceTypes)
        {
            services.Should().Contain(s => s.ServiceType == type,
                $"Service {type.Name} should be registered");
        }
    }

    [Test]
    public void AddRepositories_RegistersAllRepositoryTypes()
    {
        var services = new ServiceCollection();
        services.AddRepositories();

        var repoTypes = new[]
        {
            typeof(IUserRepository),
            typeof(IPuchasedRepository),
            typeof(ITournamentRepository),
            typeof(IMaterialsRepository),
            typeof(ICuerdasRepository),
            typeof(IExportRepository),
            typeof(IExcelRepository),
        };

        foreach (var type in repoTypes)
        {
            services.Should().Contain(s => s.ServiceType == type,
                $"Repository {type.Name} should be registered");
        }
    }

    [Test]
    public void AddRateLimitingPolicy_ConfiguresRateLimiting()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRateLimitingPolicy();

        using var provider = services.BuildServiceProvider();
        var counterStore = provider.GetService<AspNetCoreRateLimit.IRateLimitCounterStore>();
        counterStore.Should().NotBeNull();
    }

    [Test]
    public void AddRealtimeSignalR_RegistersSignalR()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRealtimeSignalR();

        using var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();
    }

    [Test]
    public void AddMvcControllers_RegistersControllers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcControllers();

        using var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();
    }

    [Test]
    public void AddDatabase_RegistersDbContexts()
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
    public void AddCache_InDevelopment_RegistersMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Development"] = "true"
            })
            .Build();

        services.AddCache(config);

        using var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<MemoryCacheService>();
    }

    [Test]
    public void AddCloudinary_WithValidConfig_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "test-cloud",
                ["Cloudinary:ApiKey"] = "test-key",
                ["Cloudinary:ApiSecret"] = "test-secret"
            })
            .Build();

        services.AddCloudinary(config);

        using var provider = services.BuildServiceProvider();
        var cloudinary = provider.GetService<ICloudinaryService>();
        cloudinary.Should().NotBeNull();
    }

    [Test]
    public void AddCloudinary_WithoutCredentials_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => services.AddCloudinary(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cloudinary*");
    }

    [Test]
    public void AddAuthentication_WithValidJwtKey_RegistersAuth()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-test-key-that-is-long-enough-for-hmac-sha256"
            })
            .Build();

        services.AddAuthentication(config);

        using var provider = services.BuildServiceProvider();
        var authService = provider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authService.Should().NotBeNull();
    }

    [Test]
    public void AddAuthentication_WithoutJwtKey_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => services.AddAuthentication(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT Key*");
    }

    [Test]
    public void AddAppConfig_SetsCurrentAppConfig()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:FrontendUrl"] = "https://test.example.com",
                ["App:ServerUrl"] = "https://api.example.com"
            })
            .Build();

        services.AddAppConfig(config);

        AppConfig.Current.Should().NotBeNull();
        AppConfig.Current.FrontendUrl.Should().Be("https://test.example.com");
    }

    [Test]
    public void AddWhatsAppHttpClient_RegistersClient()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:WhatsAppAccessToken"] = "test-token"
            })
            .Build();

        services.AddWhatsAppHttpClient(config);

        using var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetService<System.Net.Http.IHttpClientFactory>();
        clientFactory.Should().NotBeNull();
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
}
