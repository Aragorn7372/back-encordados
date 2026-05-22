using BackEncordados.Infraestructure;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TestEncordados.Unit.Infrastructure;

public class CorsConfigTests
{
    [Test]
    public void AddCorsPolicy_InDevelopment_RegistersAllowAllPolicy()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddLogging();
        services.AddCorsPolicy(config, isDevelopment: true);

        var provider = services.BuildServiceProvider();
        var cors = provider.GetRequiredService<ICorsService>();
        cors.Should().NotBeNull();
    }

    [Test]
    public void AddCorsPolicy_InProduction_WithoutOrigins_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddCorsPolicy(config, isDevelopment: false);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CorsOptions>>();
        var act = () => _ = options.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cors:AllowedOrigins*");
    }

    [Test]
    public void AddCorsPolicy_InProduction_WithOrigins_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://example.com"
            })
            .Build();

        services.AddCorsPolicy(config, isDevelopment: false);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = options.Value.GetPolicy("ProductionPolicy");
        policy.Should().NotBeNull();
    }
}
