using BackEncordados.Common.SignalR;

namespace BackEncordados.Infraestructure;


public static class SignalRExtensions
{

    public static IServiceCollection AddRealtimeSignalR(this IServiceCollection services) {
        services.AddSignalR()
            .AddHubOptions<SignalHub>(options => {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 4;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

        return services;
    }


    public static IApplicationBuilder MapSignalRHubs(this IApplicationBuilder app)
    {
        var webApp = (WebApplication)app;
        
        webApp.MapHub<SignalHub>("/hub/Torneos");

        return app;
    }
}