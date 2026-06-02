using Serilog;
using Serilog.Events;

namespace BackEncordados.Infraestructure;

public static class SerilogConfig
{
    public static LoggerConfiguration Configure()
    {
        var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .WriteTo.Sink(new SanitizingSink(outputTemplate, null));
    }
}
