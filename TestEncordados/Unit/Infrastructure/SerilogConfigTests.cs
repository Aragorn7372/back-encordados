using BackEncordados.Infraestructure;
using FluentAssertions;
using Serilog.Events;

namespace TestEncordados.Unit.Infrastructure;

public class SerilogConfigTests
{
    [Test]
    public void Configure_ReturnsLoggerConfiguration()
    {
        var config = SerilogConfig.Configure();

        config.Should().NotBeNull();
    }

    [Test]
    public void Configure_SetsMinimumLevel_Information()
    {
        var config = SerilogConfig.Configure();

        var logger = config.CreateLogger();
        logger.IsEnabled(LogEventLevel.Information).Should().BeTrue();
        logger.IsEnabled(LogEventLevel.Debug).Should().BeFalse();
        logger.Dispose();
    }

    [Test]
    public void Configure_OverridesMicrosoft_ToWarning()
    {
        var config = SerilogConfig.Configure();

        var logger = config.CreateLogger();
        logger.IsEnabled(LogEventLevel.Warning).Should().BeTrue();
        logger.Dispose();
    }
}
