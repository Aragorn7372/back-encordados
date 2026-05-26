using BackEncordados.Common.Service.Cloudinary;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración del servicio de Cloudinary para gestión de imágenes.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IServiceCollection</c>
/// que lee la configuración de Cloudinary desde <c>appsettings.json</c>,
/// asigna las opciones a <see cref="CloudinaryOptions.Current"/> y registra
/// el servicio <see cref="ICloudinaryService"/> en el contenedor de DI.</para>
/// <para><b>Mapeo con appsettings.json:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Clave en JSON</term>
///     <description>Propiedad</description>
///     <description>Requerido</description>
///     <description>Default si falta</description>
///   </listheader>
///   <item>
///     <term>Cloudinary:CloudName</term>
///     <description>CloudName</description>
///     <description>Sí</description>
///     <description>— (lanza excepción)</description>
///   </item>
///   <item>
///     <term>Cloudinary:ApiKey</term>
///     <description>ApiKey</description>
///     <description>Sí</description>
///     <description>— (lanza excepción)</description>
///   </item>
///   <item>
///     <term>Cloudinary:ApiSecret</term>
///     <description>ApiSecret</description>
///     <description>Sí</description>
///     <description>— (lanza excepción)</description>
///   </item>
///   <item>
///     <term>Cloudinary:Transformations:Width</term>
///     <description>Transformations.Width</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_WIDTH</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:Transformations:Height</term>
///     <description>Transformations.Height</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_HEIGHT</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:Transformations:Crop</term>
///     <description>Transformations.Crop</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_CROP</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:Transformations:Quality</term>
///     <description>Transformations.Quality</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_QUALITY</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:Transformations:Format</term>
///     <description>Transformations.Format</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_FORMAT</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:DefaultImages:Usuarios</term>
///     <description>DefaultImages.Usuarios</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_IMAGE_USUARIOS</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:DefaultImages:Talleres</term>
///     <description>DefaultImages.Talleres</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_IMAGE_TALLERES</c></description>
///   </item>
///   <item>
///     <term>Cloudinary:DefaultImages:Materies</term>
///     <description>DefaultImages.Materies</description>
///     <description>No</description>
///     <description><c>CloudinaryConstants.DEFAULT_IMAGE_MATERIALES</c></description>
///   </item>
/// </list>
/// <para>Usar <c>services.AddCloudinary(configuration)</c> en <c>Program.cs</c>.</para>
/// </remarks>
public static class CloudinaryConfig {

    /// <summary>
    /// Configura el servicio de Cloudinary y registra <see cref="ICloudinaryService"/> en DI.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Lee la sección <c>"Cloudinary"</c> de <paramref name="configuration"/>.</description></item>
    ///   <item><description>Si la sección no existe, registra una advertencia con Serilog
    ///   (no es fatal, pero el servicio puede fallar después).</description></item>
    ///   <item><description>Lee <c>CloudName</c>, <c>ApiKey</c> y <c>ApiSecret</c> de la sección.</description></item>
    ///   <item><description>Si alguna credencial es nula o vacía, lanza <c>InvalidOperationException</c>
    ///   con instrucciones de configuración.</description></item>
    ///   <item><description>Parsea las transformaciones de imagen (<c>Width</c>, <c>Height</c>,
    ///   <c>Crop</c>, <c>Quality</c>, <c>Format</c>) con fallback a constantes de
    ///   <see cref="CloudinaryConstants"/> cuando el valor no está presente o no es parseable
    ///   (usa <c>int.TryParse</c> para Width/Height).</description></item>
    ///   <item><description>Parsea las imágenes predeterminadas (<c>Usuarios</c>, <c>Talleres</c>,
    ///   <c>Materies</c>) con fallback a constantes de <see cref="CloudinaryConstants"/>.</description></item>
    ///   <item><description>Asigna la instancia a <c>CloudinaryOptions.Current</c> para acceso global.</description></item>
    ///   <item><description>Registra <c>ICloudinaryService</c> como <c>Scoped</c> mediante
    ///   <c>TryAddScoped</c> (solo si no hay otro registro previo).</description></item>
    ///   <item><description>Registra confirmación en log con CloudName y dimensiones de transformación.</description></item>
    /// </list>
    /// <para><b>Nota técnica:</b> Se usa <c>TryAddScoped</c> en lugar de <c>AddScoped</c>
    /// para permitir que en los tests se pueda sobrescribir el servicio con un mock
    /// sin conflictos de registro duplicado.</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <param name="configuration">Configuración de la aplicación (IConfiguration root).</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
    /// <exception cref="InvalidOperationException">CloudName, ApiKey o ApiSecret no están
    /// configurados en appsettings.json.</exception>
    public static IServiceCollection AddCloudinary(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Configurando servicio Cloudinary...");

        var cloudinarySection = configuration.GetSection("Cloudinary");

        if (!cloudinarySection.Exists())
        {
            Log.Warning("Sección 'Cloudinary' no encontrada en configuración. Cloudinary puede no funcionar correctamente.");
        }

        var cloudName = cloudinarySection["CloudName"];
        var apiKey = cloudinarySection["ApiKey"];
        var apiSecret = cloudinarySection["ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Credenciales de Cloudinary incompletas. " +
                "Asegúrate de configurar Cloudinary:CloudName, Cloudinary:ApiKey y Cloudinary:ApiSecret en appsettings.json");
        }

        var options = new CloudinaryOptions
        {
            CloudName = cloudName,
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            Transformations = new TransformationOptions
            {
                Width = int.TryParse(cloudinarySection["Transformations:Width"], out var w) ? w : CloudinaryConstants.DEFAULT_WIDTH,
                Height = int.TryParse(cloudinarySection["Transformations:Height"], out var h) ? h : CloudinaryConstants.DEFAULT_HEIGHT,
                Crop = cloudinarySection["Transformations:Crop"] ?? CloudinaryConstants.DEFAULT_CROP,
                Quality = cloudinarySection["Transformations:Quality"] ?? CloudinaryConstants.DEFAULT_QUALITY,
                Format = cloudinarySection["Transformations:Format"] ?? CloudinaryConstants.DEFAULT_FORMAT
            },
            DefaultImages = new DefaultImageOptions
            {
                Usuarios = cloudinarySection["DefaultImages:Usuarios"] ?? CloudinaryConstants.DEFAULT_IMAGE_USUARIOS,
                Talleres = cloudinarySection["DefaultImages:Talleres"] ?? CloudinaryConstants.DEFAULT_IMAGE_TALLERES,
                Materies = cloudinarySection["DefaultImages:Materies"] ?? CloudinaryConstants.DEFAULT_IMAGE_MATERIALES
            }
        };

        CloudinaryOptions.Current = options;
        services.TryAddScoped<ICloudinaryService, CloudinaryService>();

        Log.Information("Servicio Cloudinary configurado exitosamente.");
        Log.Information("Cloud: {CloudName}, Transformaciones: {Width}x{Height} ({Crop})",
            options.CloudName, options.Transformations.Width, options.Transformations.Height, options.Transformations.Crop);

        return services;
    }
}


