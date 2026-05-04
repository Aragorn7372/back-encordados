using System.Text;
using BackEncordados.Infraestructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

/// <summary>
/// Punto de entrada de la aplicación API REST y GraphQL.
/// Configura servicios, pipeline de middlewares y arranque.
/// </summary>

Log.Logger= SerilogConfig.Configure().CreateLogger();
Console.OutputEncoding = Encoding.UTF8; 
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var env = builder.Environment;
//configuracion log
builder.Host.UseSerilog();
var services = builder.Services;
// negociacion de serializables
services.AddMvcControllers();

//base de datos en possgress
services.AddDatabase(configuration);
// Auth
services.AddAuthentication(builder.Configuration);
// repositorios
services.AddRepositories();
// servicios
services.AddServices();
// cache
services.AddCache(configuration);
services.AddEmail(builder.Environment);


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();


app.UseCorsPolicy();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
await app.InitializeDatabaseAsync();

try
{
    Log.Information("Iniciando aplicación FunkoApi...");
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