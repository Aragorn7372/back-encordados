using BackEncordados.Common.Service.Cloudinary;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace BackEncordados.Infraestructure;


public static class CloudinaryConfig {

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


