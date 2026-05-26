using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BackEncordados.Common.Exceptions;

namespace BackEncordados.Common.Service.Cloudinary;


/// <summary>
/// Implementación de <see cref="ICloudinaryService"/> para gestión de imágenes en Cloudinary.
/// </summary>
/// <remarks>
/// <para>Servicio que encapsula las operaciones de subida, obtención de URL y eliminación
/// de imágenes en Cloudinary. Utiliza una instancia singleton de <c>CloudinaryDotNet.Cloudinary</c>
/// inicializada con las credenciales de <see cref="CloudinaryOptions.Current"/>.</para>
///
/// <para><b>Arquitectura:</b></para>
/// <list type="bullet">
///   <item><description><b>Singleton lazy:</b> La instancia de <c>CloudinaryDotNet.Cloudinary</c>
///   se inicializa en la primera llamada mediante <see cref="GetCloudinary"/>.</description></item>
///   <item><description><b>Tolerancia a fallos:</b> Las operaciones de subida y eliminación envuelven
///   llamadas a Cloudinary en try/catch con logging, relanzando como excepciones específicas
///   (<see cref="CloudinaryUploadException"/>, <see cref="CloudinaryDeleteException"/>).</description></item>
///   <item><description><b>Validación de carpetas:</b> Solo se permiten tres carpetas predefinidas:
///   <c>"usuarios"</c>, <c>"talleres"</c> y <c>"materias"</c>.</description></item>
/// </list>
///
/// <para><b>Diferencias entre métodos de subida:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Característica</term>
///     <description><c>UploadAsync</c></description>
///     <description><c>UploadWithAutoNameAsync</c></description>
///   </listheader>
///   <item>
///     <term>Nombre</term>
///     <description>Explícito (parámetro <c>filename</c>)</description>
///     <description>Auto-generado (<c>entityId_timestamp</c>)</description>
///   </item>
///   <item>
///     <term>Overwrite</term>
///     <description><c>true</c></description>
///     <description><c>false</c></description>
///   </item>
///   <item>
///     <term>Retorno</term>
///     <description>Solo PublicId (string)</description>
///     <description><see cref="CloudinaryUploadResult"/> completo</description>
///   </item>
/// </list>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones y errores.</param>
public class CloudinaryService(ILogger<CloudinaryService> logger) : ICloudinaryService
{
    private static CloudinaryDotNet.Cloudinary? _cloudinary;

    /// <summary>
    /// Obtiene (o inicializa) la instancia singleton de <c>CloudinaryDotNet.Cloudinary</c>.
    /// </summary>
    /// <remarks>
    /// <para>Inicialización lazy con doble comprobación. Si <see cref="CloudinaryOptions.Current"/>
    /// es <c>null</c>, lanza <see cref="CloudinaryConfigurationException"/> indicando que falta
    /// la configuración en <c>Program.cs</c>.</para>
    /// </remarks>
    /// <returns>Instancia configurada de <c>CloudinaryDotNet.Cloudinary</c>.</returns>
    private static CloudinaryDotNet.Cloudinary GetCloudinary()
    {
        if (_cloudinary == null)
        {
            if (CloudinaryOptions.Current == null)
            {
                throw new CloudinaryConfigurationException("CloudinaryOptions no ha sido inicializado. Asegúrate de llamar a AddCloudinary() en Program.cs");
            }

            var account = new Account(
                CloudinaryOptions.Current.CloudName,
                CloudinaryOptions.Current.ApiKey,
                CloudinaryOptions.Current.ApiSecret
            );
            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
        }

        return _cloudinary;
    }
    
    /// <summary>
    /// Obtiene la URL transformada de una imagen por su nombre de archivo y carpeta.
    /// </summary>
    /// <remarks>
    /// <para>Si <paramref name="filename"/> es <c>null</c> o vacío, delega en
    /// <see cref="GetDefaultImageUrl"/> para retornar la imagen predeterminada de la carpeta.</para>
    /// <para>Si <paramref name="filename"/> tiene valor, construye el PublicId como
    /// <c>"{folder}/{filename}"</c> y aplica transformaciones vía <see cref="BuildTransformedUrl"/>.</para>
    /// </remarks>
    /// <param name="filename">Nombre del archivo (sin carpeta). Si vacío, se usa la imagen por defecto.</param>
    /// <param name="folder">Carpeta Cloudinary (usuarios, talleres, materias).</param>
    /// <returns>URL segura con transformaciones aplicadas.</returns>
    public string GetImageUrl(string filename, string folder)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return GetDefaultImageUrl(folder);
        }

        var publicId = $"{folder}/{filename}";
        return BuildTransformedUrl(publicId);
    }

    /// <summary>
    /// Obtiene la URL de la imagen predeterminada para una carpeta.
    /// </summary>
    /// <remarks>
    /// <para>Mapea la carpeta a su Public ID predeterminado mediante una expresión switch
    /// sobre los valores de <see cref="CloudinaryConstants"/>:</para>
    /// <list type="bullet">
    ///   <item><description><c>"usuarios"</c> → <c>CloudinaryOptions.Current.DefaultImages.Usuarios</c></description></item>
    ///   <item><description><c>"talleres"</c> → <c>CloudinaryOptions.Current.DefaultImages.Talleres</c></description></item>
    ///   <item><description><c>"materias"</c> → <c>CloudinaryOptions.Current.DefaultImages.Materies</c></description></item>
    ///   <item><description>Carpeta no válida → <see cref="CloudinaryInvalidParameterException"/></description></item>
    /// </list>
    /// </remarks>
    /// <param name="folder">Carpeta Cloudinary.</param>
    /// <returns>URL segura de la imagen predeterminada.</returns>
    public string GetDefaultImageUrl(string folder)
    {
        if (CloudinaryOptions.Current == null)
        {
            throw new CloudinaryConfigurationException("CloudinaryOptions no ha sido inicializado");
        }

        var defaultPublicId = folder.ToLower() switch
        {
            CloudinaryConstants.FOLDER_USUARIOS => CloudinaryOptions.Current.DefaultImages.Usuarios,
            CloudinaryConstants.FOLDER_TALLERES => CloudinaryOptions.Current.DefaultImages.Talleres,
            CloudinaryConstants.FOLDER_MATERIES => CloudinaryOptions.Current.DefaultImages.Materies,
            _ => throw new CloudinaryInvalidParameterException($"Carpeta no válida: {folder}")
        };

        return BuildTransformedUrl(defaultPublicId);
    }
    
    /// <summary>
    /// Sube una imagen a Cloudinary con nombre de archivo explícito (sobrescribe si existe).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida que el archivo y el nombre no sean nulos/vacíos.</description></item>
    ///   <item><description>Valida que la carpeta esté entre las permitidas (<see cref="ValidateFolder"/>).</description></item>
    ///   <item><description>Abre el stream del archivo y configura <c>ImageUploadParams</c> con
    ///   PublicId = <c>"{folder}/{filename}"</c> y <c>Overwrite = true</c>.</description></item>
    ///   <item><description>Ejecuta la subida asincrónica. Si hay error en la respuesta de Cloudinary,
    ///   lanza <see cref="CloudinaryUploadException"/>.</description></item>
    ///   <item><description>Cualquier excepción de red/API se captura y relanza como <see cref="CloudinaryUploadException"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="file">Archivo de imagen desde el formulario HTTP.</param>
    /// <param name="filename">Nombre deseado para el archivo (sin carpeta).</param>
    /// <param name="folder">Carpeta de destino (usuarios, talleres, materias).</param>
    /// <returns>El PublicId completo de la imagen subida (ej: <c>"usuarios/mi-foto.jpg"</c>).</returns>
    public async Task<string> UploadAsync(IFormFile file, string filename, string folder)
    {
        if (file is null || file.Length == 0)
        {
            throw new CloudinaryInvalidParameterException("El archivo es requerido");
        }

        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new CloudinaryInvalidParameterException("El nombre de archivo es requerido");
        }

        ValidateFolder(folder);

        try
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = $"{folder}/{filename}",
                Folder = folder,
                Overwrite = true
            };

            var uploadResult = await GetCloudinary().UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                logger.LogError("Error al subir imagen a Cloudinary: {Error}", uploadResult.Error.Message);
                throw new CloudinaryUploadException(uploadResult.Error.Message);
            }

            logger.LogInformation("Imagen subida exitosamente: {PublicId}", uploadResult.PublicId);
            return uploadResult.PublicId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción al subir imagen a Cloudinary");
            throw new CloudinaryUploadException(ex.Message, ex);
        }
    }
    
    /// <summary>
    /// Elimina una imagen de Cloudinary por su Public ID.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida que el PublicId no sea nulo/vacío.</description></item>
    ///   <item><description>Configura <c>DeletionParams</c> con <c>ResourceType = Image</c>.</description></item>
    ///   <item><description>Ejecuta <c>DestroyAsync</c>. Si hay error en la respuesta, lanza
    ///   <see cref="CloudinaryDeleteException"/>.</description></item>
    ///   <item><description>Cualquier excepción de red/API se captura y relanza como <see cref="CloudinaryDeleteException"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="publicId">Public ID completo de la imagen a eliminar.</param>
    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new CloudinaryInvalidParameterException("El public ID es requerido");
        }

        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var deleteResult = await GetCloudinary().DestroyAsync(deleteParams);

            if (deleteResult.Error != null)
            {
                logger.LogError("Error al eliminar imagen de Cloudinary: {Error}", deleteResult.Error.Message);
                throw new CloudinaryDeleteException(deleteResult.Error.Message);
            }

            logger.LogInformation("Imagen eliminada exitosamente: {PublicId}", publicId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción al eliminar imagen de Cloudinary: {PublicId}", publicId);
            throw new CloudinaryDeleteException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Sube una imagen con nombre de archivo auto-generado (no sobrescribe si existe).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida archivo, entityId y carpeta.</description></item>
    ///   <item><description>Genera nombre único via <see cref="GenerateFilename"/> con formato
    ///   <c>"{entityId}_{yyyyMMdd_HHmmss}"</c>.</description></item>
    ///   <item><description>Configura <c>ImageUploadParams</c> con <c>Overwrite = false</c>
    ///   (no sobrescribe si el archivo ya existe).</description></item>
    ///   <item><description>Ejecuta subida y retorna <see cref="CloudinaryUploadResult"/>
    ///   con PublicId, SecureUrl y nombre generado.</description></item>
    /// </list>
    /// <para>Las excepciones <see cref="CloudinaryException"/> se relanzan sin envolver;
    /// otras excepciones se envuelven en <see cref="CloudinaryUploadException"/>.</para>
    /// </remarks>
    /// <param name="file">Archivo de imagen desde el formulario HTTP.</param>
    /// <param name="entityId">ID de la entidad asociada (ej: UserId, TournamentId).</param>
    /// <param name="folder">Carpeta de destino (usuarios, talleres, materias).</param>
    /// <returns><see cref="CloudinaryUploadResult"/> con los datos de la imagen subida.</returns>
    public async Task<CloudinaryUploadResult> UploadWithAutoNameAsync(IFormFile file, string entityId, string folder)
    {
        if (file is null || file.Length == 0)
        {
            throw new CloudinaryInvalidParameterException("El archivo es requerido");
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new CloudinaryInvalidParameterException("El ID de entidad es requerido");
        }

        ValidateFolder(folder);

        try
        {
            var generatedFilename = GenerateFilename(entityId);

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = generatedFilename,
                Folder = folder,
                Overwrite = false  
            };

            var uploadResult = await GetCloudinary().UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                logger.LogError("Error al subir imagen a Cloudinary: {Error}", uploadResult.Error.Message);
                throw new CloudinaryUploadException(uploadResult.Error.Message);
            }

            logger.LogInformation("Imagen subida exitosamente con nombre generado: {PublicId}", uploadResult.PublicId);

            return new CloudinaryUploadResult
            {
                PublicId = uploadResult.PublicId,
                ImageUrl = uploadResult.SecureUrl.AbsoluteUri,
                GeneratedFilename = generatedFilename
            };
        }
        catch (CloudinaryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción al subir imagen a Cloudinary");
            throw new CloudinaryUploadException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Obtiene la URL de una imagen con transformaciones personalizadas.
    /// </summary>
    /// <remarks>
    /// <para>Construye la URL usando <c>Cloudinary.Api.UrlImgUp</c> con una cadena de
    /// transformaciones fluida:</para>
    /// <list type="bullet">
    ///   <item><description><b>Width:</b> <paramref name="width"/> o <c>CloudinaryOptions.Current.Transformations.Width</c></description></item>
    ///   <item><description><b>Height:</b> <paramref name="height"/> o <c>CloudinaryOptions.Current.Transformations.Height</c></description></item>
    ///   <item><description><b>Crop:</b> <c>CloudinaryOptions.Current.Transformations.Crop</c> (default: <c>"fill"</c>)</description></item>
    ///   <item><description><b>Quality:</b> <c>CloudinaryOptions.Current.Transformations.Quality</c> (default: <c>"auto"</c>)</description></item>
    ///   <item><description><b>FetchFormat:</b> <c>"auto"</c> (Cloudinary elige WebP/AVIF según el navegador)</description></item>
    /// </list>
    /// <para>La URL se genera con <c>Secure = true</c> (HTTPS) y <c>ResourceType = "image"</c>.</para>
    /// </remarks>
    /// <param name="publicId">Public ID de la imagen.</param>
    /// <param name="width">Ancho en píxeles (opcional, default: configurado en <c>CloudinaryOptions</c>).</param>
    /// <param name="height">Alto en píxeles (opcional, default: configurado en <c>CloudinaryOptions</c>).</param>
    /// <returns>URL segura con transformaciones.</returns>
    public string GetImageUrlWithTransformations(string publicId, int? width = null, int? height = null)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new CloudinaryInvalidParameterException("El public ID es requerido");
        }

        if (CloudinaryOptions.Current == null)
        {
            throw new CloudinaryConfigurationException("CloudinaryOptions no ha sido inicializado");
        }

        var w = width ?? CloudinaryOptions.Current.Transformations.Width;
        var h = height ?? CloudinaryOptions.Current.Transformations.Height;

        return GetCloudinary().Api.UrlImgUp
            .Transform(new Transformation()
                .Width(w)
                .Height(h)
                .Crop(CloudinaryOptions.Current.Transformations.Crop)
                .Quality(CloudinaryOptions.Current.Transformations.Quality)
                .FetchFormat("auto"))
            .ResourceType("image")
            .Secure(true)
            .BuildUrl(publicId);
    }
    
    /// <summary>
    /// Resuelve el valor almacenado en base de datos a una URL completa de imagen.
    /// </summary>
    /// <remarks>
    /// <para>Lógica de resolución:</para>
    /// <list type="bullet">
    ///   <item><description>Si <paramref name="storedValue"/> es vacío → <see cref="GetDefaultImageUrl"/> (imagen por defecto).</description></item>
    ///   <item><description>Si comienza con <c>"http"</c> → se retorna tal cual (URL externa o absoluta guardada previamente).</description></item>
    ///   <item><description>En cualquier otro caso → se trata como PublicId y se aplican transformaciones vía
    ///   <see cref="GetImageUrlWithTransformations"/>.</description></item>
    /// </list>
    /// <para>Este método es útil para servicios que leen el valor de imagen desde la base de datos
    /// y necesitan normalizarlo a una URL renderizable.</para>
    /// </remarks>
    /// <param name="storedValue">Valor almacenado en BD (vacío, URL completa, o PublicId).</param>
    /// <param name="folder">Carpeta Cloudinary para obtener imagen por defecto si es necesario.</param>
    /// <returns>URL resuelta.</returns>
    public string ResolveImageUrl(string storedValue, string folder)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return GetDefaultImageUrl(folder);
        }

        if (storedValue.StartsWith("http"))
        {
            return storedValue;
        }

        return GetImageUrlWithTransformations(storedValue);
    }
    
    /// <summary>
    /// Genera un nombre de archivo único basado en el ID de entidad y timestamp actual.
    /// </summary>
    /// <param name="entityId">ID de la entidad (ej: Ulid como string).</param>
    /// <returns>Nombre con formato <c>"{entityId}_{yyyyMMdd_HHmmss}"</c>.</returns>
    private static string GenerateFilename(string entityId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{entityId}_{timestamp}";
    }

    /// <summary>
    /// Construye una URL transformada para un PublicId usando los valores de configuración por defecto.
    /// </summary>
    /// <param name="publicId">Public ID de la imagen.</param>
    /// <returns>URL segura con transformaciones.</returns>
    private string BuildTransformedUrl(string publicId)
    {
        return GetImageUrlWithTransformations(publicId);
    }
    
    /// <summary>
    /// Valida que la carpeta esté entre las permitidas por el sistema.
    /// </summary>
    /// <remarks>
    /// <para>Carpetas válidas:</para>
    /// <list type="bullet">
    ///   <item><description><c>"usuarios"</c> — <see cref="CloudinaryConstants.FOLDER_USUARIOS"/></description></item>
    ///   <item><description><c>"talleres"</c> — <see cref="CloudinaryConstants.FOLDER_TALLERES"/></description></item>
    ///   <item><description><c>"materias"</c> — <see cref="CloudinaryConstants.FOLDER_MATERIES"/></description></item>
    /// </list>
    /// <para>Si la carpeta no coincide con ninguna, lanza <see cref="CloudinaryInvalidParameterException"/>.</para>
    /// </remarks>
    /// <param name="folder">Carpeta a validar.</param>
    private static void ValidateFolder(string folder)
    {
        var validFolders = new[] { CloudinaryConstants.FOLDER_USUARIOS, CloudinaryConstants.FOLDER_TALLERES, CloudinaryConstants.FOLDER_MATERIES };
        if (!validFolders.Contains(folder.ToLower()))
        {
            throw new CloudinaryInvalidParameterException($"Carpeta no válida. Debe ser: {string.Join(", ", validFolders)}");
        }
    }
}


/// <summary>
/// Opciones de configuración para el servicio Cloudinary.
/// </summary>
/// <remarks>
/// <para>Configuración singleton inyectada mediante <c>AddCloudinary()</c> en <c>Program.cs</c>.
/// Se expone a través de <see cref="Current"/> para acceso estático desde <see cref="CloudinaryService"/>.</para>
/// </remarks>
public class CloudinaryOptions
{
    /// <summary>Instancia singleton de la configuración activa de Cloudinary.</summary>
    public static CloudinaryOptions? Current { get; set; }

    /// <summary>Nombre del cloud en Cloudinary (parte del URL: https://res.cloudinary.com/{CloudName}/).</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>API Key de Cloudinary para autenticación.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>API Secret de Cloudinary para autenticación.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Opciones de transformación por defecto para imágenes.</summary>
    public TransformationOptions Transformations { get; set; } = new();

    /// <summary>Public IDs de imágenes predeterminadas por módulo.</summary>
    public DefaultImageOptions DefaultImages { get; set; } = new();
}


/// <summary>
/// Opciones de transformación por defecto para imágenes servidas desde Cloudinary.
/// </summary>
public class TransformationOptions
{
    /// <summary>Ancho en píxeles (default: <c>800</c>).</summary>
    public int Width { get; set; } = CloudinaryConstants.DEFAULT_WIDTH;

    /// <summary>Alto en píxeles (default: <c>600</c>).</summary>
    public int Height { get; set; } = CloudinaryConstants.DEFAULT_HEIGHT;

    /// <summary>Modo de recorte (default: <c>"fill"</c>).</summary>
    public string Crop { get; set; } = CloudinaryConstants.DEFAULT_CROP;

    /// <summary>Calidad de imagen (default: <c>"auto"</c>).</summary>
    public string Quality { get; set; } = CloudinaryConstants.DEFAULT_QUALITY;

    /// <summary>Formato de imagen (default: <c>"auto"</c>).</summary>
    public string Format { get; set; } = CloudinaryConstants.DEFAULT_FORMAT;
}

/// <summary>
/// Public IDs de imágenes predeterminadas para cada módulo del sistema.
/// </summary>
public class DefaultImageOptions
{
    /// <summary>Public ID de la imagen predeterminada para usuarios.</summary>
    public string Usuarios { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS;

    /// <summary>Public ID de la imagen predeterminada para talleres/torneos.</summary>
    public string Talleres { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_TALLERES;

    /// <summary>Public ID de la imagen predeterminada para materiales.</summary>
    public string Materies { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_MATERIALES;
}

