namespace BackEncordados.Common.Service.Cloudinary;


/// <summary>
/// Define el contrato para el servicio de gestión de imágenes en Cloudinary.
/// </summary>
/// <remarks>
/// <para>Proporciona operaciones de subida, obtención de URL y eliminación de imágenes
/// almacenadas en Cloudinary. Todas las imágenes se organizan en carpetas predefinidas
/// (usuarios, talleres, materias) y se sirven con transformaciones optimizadas.</para>
///
/// <para><b>Operaciones:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Operación</term>
///     <description>Método</description>
///     <description>Excepción en error</description>
///   </listheader>
///   <item>
///     <term>Subir con nombre explícito</term>
///     <description><c>UploadAsync</c></description>
///     <description><see cref="CloudinaryUploadException"/></description>
///   </item>
///   <item>
///     <term>Subir con nombre auto-generado</term>
///     <description><c>UploadWithAutoNameAsync</c></description>
///     <description><see cref="CloudinaryUploadException"/></description>
///   </item>
///   <item>
///     <term>Eliminar</term>
///     <description><c>DeleteAsync</c></description>
///     <description><see cref="CloudinaryDeleteException"/></description>
///   </item>
///   <item>
///     <term>Obtener URL con transformaciones</term>
///     <description><c>GetImageUrl</c>, <c>GetImageUrlWithTransformations</c></description>
///     <description><see cref="CloudinaryConfigurationException"/> si no hay configuración</description>
///   </item>
///   <item>
///     <term>Resolver valor almacenado</term>
///     <description><c>ResolveImageUrl</c></description>
///     <description><see cref="CloudinaryConfigurationException"/> si no hay configuración</description>
///   </item>
/// </list>
///
/// <para>Si no se especifica configuración en <c>CloudinaryOptions.Current</c>, todos los métodos
/// que generan URL lanzan <see cref="CloudinaryConfigurationException"/>.</para>
/// </remarks>
public interface ICloudinaryService
{
    /// <summary>
    /// Obtiene la URL transformada de una imagen por su nombre de archivo y carpeta.
    /// </summary>
    /// <remarks>
    /// <para>Si <paramref name="filename"/> es <c>null</c> o vacío, retorna la imagen
    /// predeterminada de la carpeta correspondiente mediante <see cref="GetDefaultImageUrl"/>.</para>
    /// </remarks>
    /// <param name="filename">Nombre del archivo (sin carpeta). Si es vacío, se usa la imagen por defecto.</param>
    /// <param name="folder">Carpeta Cloudinary (ej: <c>"usuarios"</c>, <c>"talleres"</c>, <c>"materias"</c>).</param>
    /// <returns>URL segura (HTTPS) con transformaciones aplicadas.</returns>
    string GetImageUrl(string filename, string folder);

    /// <summary>
    /// Obtiene la URL de la imagen predeterminada para una carpeta.
    /// </summary>
    /// <remarks>
    /// <para>Mapea la carpeta al Public ID configurado en <c>CloudinaryOptions.DefaultImages</c>.
    /// Si la carpeta no es válida, lanza <see cref="CloudinaryInvalidParameterException"/>.</para>
    /// </remarks>
    /// <param name="folder">Carpeta Cloudinary (usuarios, talleres o materias).</param>
    /// <returns>URL segura de la imagen predeterminada con transformaciones.</returns>
    string GetDefaultImageUrl(string folder);

    /// <summary>
    /// Sube una imagen a Cloudinary con un nombre de archivo explícito.
    /// </summary>
    /// <remarks>
    /// <para>La imagen se almacena con PublicId = <c>"{folder}/{filename}"</c> y
    /// <c>Overwrite = true</c> (sobrescribe si ya existe).</para>
    /// </remarks>
    /// <param name="file">Archivo de imagen desde el formulario HTTP.</param>
    /// <param name="filename">Nombre deseado para el archivo en Cloudinary.</param>
    /// <param name="folder">Carpeta de destino.</param>
    /// <returns>El PublicId completo de la imagen subida.</returns>
    Task<string> UploadAsync(IFormFile file, string filename, string folder);

    /// <summary>
    /// Sube una imagen con nombre de archivo auto-generado (timestamp + entityId).
    /// </summary>
    /// <remarks>
    /// <para>Genera un nombre único con formato <c>"{entityId}_{yyyyMMdd_HHmmss}"</c> y
    /// <c>Overwrite = false</c> para evitar sobrescribir imágenes existentes.</para>
    /// </remarks>
    /// <param name="file">Archivo de imagen desde el formulario HTTP.</param>
    /// <param name="entityId">ID de la entidad asociada (ej: UserId, TournamentId).</param>
    /// <param name="folder">Carpeta de destino.</param>
    /// <returns><see cref="CloudinaryUploadResult"/> con PublicId, URL segura y nombre generado.</returns>
    Task<CloudinaryUploadResult> UploadWithAutoNameAsync(IFormFile file, string entityId, string folder);

    /// <summary>
    /// Elimina una imagen de Cloudinary por su Public ID.
    /// </summary>
    /// <param name="publicId">Public ID completo de la imagen a eliminar.</param>
    Task DeleteAsync(string publicId);

    /// <summary>
    /// Obtiene la URL de una imagen con transformaciones personalizadas.
    /// </summary>
    /// <remarks>
    /// <para>Usa <c>Cloudinary.Api.UrlImgUp</c> con transformaciones Width, Height, Crop, Quality
    /// y FetchFormat definidas en <c>CloudinaryOptions.Current.Transformations</c>.
    /// Si no se especifican width/height, se usan los valores por defecto de configuración (800x600).</para>
    /// </remarks>
    /// <param name="publicId">Public ID de la imagen.</param>
    /// <param name="width">Ancho en píxeles (opcional, default: configurado).</param>
    /// <param name="height">Alto en píxeles (opcional, default: configurado).</param>
    /// <returns>URL segura con transformaciones.</returns>
    string GetImageUrlWithTransformations(string publicId, int? width = null, int? height = null);

    /// <summary>
    /// Resuelve el valor almacenado en base de datos a una URL completa de imagen.
    /// </summary>
    /// <remarks>
    /// <para>Lógica de resolución:</para>
    /// <list type="bullet">
    ///   <item><description>Si <paramref name="storedValue"/> es vacío → imagen predeterminada de la carpeta.</description></item>
    ///   <item><description>Si comienza con <c>"http"</c> → se devuelve tal cual (URL externa o absoluta).</description></item>
    ///   <item><description>En cualquier otro caso → se trata como PublicId y se aplican transformaciones.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="storedValue">Valor almacenado en BD (puede ser vacío, URL completa o PublicId).</param>
    /// <param name="folder">Carpeta Cloudinary para obtener imagen por defecto si es necesario.</param>
    /// <returns>URL resuelta.</returns>
    string ResolveImageUrl(string storedValue, string folder);
}


/// <summary>
/// Resultado de una operación de subida con nombre auto-generado.
/// </summary>
public class CloudinaryUploadResult
{
    /// <summary>Public ID completo de la imagen en Cloudinary.</summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>URL segura (HTTPS) de la imagen.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Nombre de archivo generado automáticamente.</summary>
    public string GeneratedFilename { get; set; } = string.Empty;
}

