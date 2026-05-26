namespace BackEncordados.Common.Service.Cloudinary;


/// <summary>
/// Constantes de configuración para el servicio Cloudinary.
/// </summary>
/// <remarks>
/// <para>Centraliza todos los valores fijos utilizados por <see cref="CloudinaryService"/>
/// para operaciones de subida, descarga y transformación de imágenes.</para>
///
/// <para><b>Grupos de constantes:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Grupo</term>
///     <description>Constantes</description>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term>Imágenes por defecto</term>
///     <description><c>DEFAULT_IMAGE_USUARIOS</c>, <c>DEFAULT_IMAGE_TALLERES</c>, <c>DEFAULT_IMAGE_MATERIALES</c></description>
///     <description>Public ID de imágenes predeterminadas mostradas cuando el usuario/entidad no tiene imagen propia subida.</description>
///   </item>
///   <item>
///     <term>Carpetas</term>
///     <description><c>FOLDER_USUARIOS</c>, <c>FOLDER_TALLERES</c>, <c>FOLDER_MATERIES</c></description>
///     <description>Nombres de carpetas en Cloudinary donde se organizan las imágenes de cada módulo.</description>
///   </item>
///   <item>
///     <term>Transformación</term>
///     <description><c>DEFAULT_WIDTH</c>, <c>DEFAULT_HEIGHT</c>, <c>DEFAULT_CROP</c>, <c>DEFAULT_QUALITY</c>, <c>DEFAULT_FORMAT</c></description>
///     <description>Parámetros por defecto usados en <c>ImageTransformation</c> para generar URLs optimizadas.</description>
///   </item>
/// </list>
///
/// <para>Las imágenes por defecto están pre-cargadas en Cloudinary mediante upload manual y
/// referenciadas por su <c>Public ID</c>. Si se cambian, debe actualizarse el Public ID en estas constantes.</para>
/// </remarks>
public static class CloudinaryConstants {
    
    // ========== Imágenes por defecto ==========

    /// <summary>Public ID de la imagen predeterminada para avatares de usuarios en Cloudinary.</summary>
    public const string DEFAULT_IMAGE_USUARIOS = "avatar-photo-default-user-icon-600nw-2558759027_y1hcma";

    /// <summary>Public ID de la imagen predeterminada para torneos/talleres en Cloudinary.</summary>
    public const string DEFAULT_IMAGE_TALLERES = "default-680x600_kw5ji6";

    /// <summary>Public ID de la imagen predeterminada para materiales en Cloudinary.</summary>
    public const string DEFAULT_IMAGE_MATERIALES = "default-680x600_kw5ji6";

    // ========== Carpetas Cloudinary ==========

    /// <summary>Nombre de la carpeta en Cloudinary para imágenes de usuarios.</summary>
    public const string FOLDER_USUARIOS = "usuarios";

    /// <summary>Nombre de la carpeta en Cloudinary para imágenes de talleres/torneos.</summary>
    public const string FOLDER_TALLERES = "talleres";

    /// <summary>Nombre de la carpeta en Cloudinary para imágenes de materiales.</summary>
    public const string FOLDER_MATERIES = "materias";

    // ========== Parámetros de transformación por defecto ==========

    /// <summary>Ancho por defecto en píxeles para las imágenes transformadas.</summary>
    public const int DEFAULT_WIDTH = 800;

    /// <summary>Alto por defecto en píxeles para las imágenes transformadas.</summary>
    public const int DEFAULT_HEIGHT = 600;

    /// <summary>Modo de recorte por defecto (<c>"fill"</c> — rellena el contenedor recortando los bordes).</summary>
    public const string DEFAULT_CROP = "fill";

    /// <summary>Calidad por defecto (<c>"auto"</c> — Cloudinary optimiza automáticamente).</summary>
    public const string DEFAULT_QUALITY = "auto";

    /// <summary>Formato por defecto (<c>"auto"</c> — Cloudinary elige el mejor formato compatible con el navegador, ej: WebP).</summary>
    public const string DEFAULT_FORMAT = "auto";
}

