using System.Text;
using BackEncordados.Infraestructure;
using BackEncordados.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;

Log.Logger= SerilogConfig.Configure().CreateLogger();
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);
// creo variables para que sea mas facil de leer
var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;
// añado configuracion de serilog para que se habilite en todos los logger
builder.Host.UseSerilog();
// añado configuracion de controllers
services.AddMvcControllers();
// añado FluentValidation (sin auto-validacion para permitir validators async)
services.AddValidatorsFromAssemblyContaining<Program>();
// añado la base de datos
services.AddDatabase(configuration);
// politicas de corps
services.AddCorsPolicy(configuration,environment.IsDevelopment());
// limite de peticiones 
services.AddRateLimitingPolicy();
// añade autorizacion
services.AddAuthorization();
// añade autenticacion
services.AddAuthentication(configuration);
// añado la cache
services.AddCache(configuration);
// añado cloudinary
services.AddCloudinary(configuration);
// añado configuracion global
services.AddAppConfig(configuration);
// añado WhatsApp HTTP client
services.AddWhatsAppHttpClient(configuration);
// añado repositorios
services.AddRepositories();
// añado servicios
services.AddServices();
//añado email service
services.AddEmail(environment);
// Registro de servicios de SignalR
services.AddRealtimeSignalR();
// declaro app
var app = builder.Build(); 
// global exception handler
app.UseGlobalExceptionHandler();
// politicas de corps (ANTES de routing para que las pre-flights pasen)
app.UseCorsPolicy();
app.UseRateLimiting();
app.UseHttpsRedirection();

// archivos estaticos ANTES de routing 
app.UseStaticFiles();
app.UseRouting();
// lo que tiene relacion con usuarios
app.UseAuthentication();
app.UseAuthorization();
// Rutas de los hubs de SignalR 
app.MapSignalRHubs();
// mapeador de controllers
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
// init de datos

await app.InitializeDatabaseAsync();
// init del storage

try
{
    Log.Information("Iniciando aplicación Back ETR..");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}