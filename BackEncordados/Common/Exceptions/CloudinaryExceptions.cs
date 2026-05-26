namespace BackEncordados.Common.Exceptions;


/// <summary>
/// Excepción base para todos los errores relacionados con Cloudinary.
/// Proporciona constructores con mensaje simple y con inner exception
/// para mantener la cadena de excepciones original.
/// </summary>
/// <remarks>
/// <para>Es la raíz de la jerarquía de excepciones de Cloudinary en el sistema.
/// No se mapea directamente a un código HTTP; en su lugar, se usan sus subtipos
/// específicos para cada escenario.</para>
/// <para><b>Subtipo y mapeo HTTP (via <see cref="GlobalExceptionHandler"/>):</b></para>
/// <list type="bullet">
///   <item><description><see cref="CloudinaryUploadException"/> → 422 CloudinaryUploadError</description></item>
///   <item><description><see cref="CloudinaryDeleteException"/> → 422 CloudinaryDeleteError</description></item>
///   <item><description><see cref="CloudinaryConfigurationException"/> → 500 CloudinaryConfigurationError</description></item>
///   <item><description><see cref="CloudinaryInvalidParameterException"/> → 400 CloudinaryInvalidParameterError</description></item>
/// </list>
/// </remarks>
public class CloudinaryException : Exception
{
    /// <summary>Crea una excepción de Cloudinary con un mensaje de error.</summary>
    /// <param name="message">Mensaje que describe el error ocurrido.</param>
    public CloudinaryException(string message) : base(message) { }

    /// <summary>Crea una excepción de Cloudinary con mensaje y excepción interna.</summary>
    /// <param name="message">Mensaje que describe el error ocurrido.</param>
    /// <param name="innerException">Excepción original que originó el error (ej: error HTTP de CloudinaryDotNet).</param>
    public CloudinaryException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Excepción que se produce cuando falla la subida de una imagen a Cloudinary.
/// </summary>
/// <remarks>
/// <para>Incluye automáticamente el prefijo "Error al subir imagen a Cloudinary: "
/// en el mensaje, concatenándolo con el mensaje específico recibido.</para>
/// <para><b>Escenarios típicos:</b></para>
/// <list type="bullet">
///   <item><description>Archivo de imagen corrupto o en formato no soportado.</description></item>
///   <item><description>Tamaño de archivo excede el límite configurado en Cloudinary.</description></item>
///   <item><description>Error de conexión con la API de Cloudinary.</description></item>
///   <item><description>Credenciales de API inválidas o expiradas.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 422 Unprocessable Entity
/// con errorType "CloudinaryUploadError".</para>
/// </remarks>
public class CloudinaryUploadException : CloudinaryException
{
    /// <summary>Crea una excepción de error de subida con mensaje específico.</summary>
    /// <param name="message">Detalle del error de subida (se concatena al prefijo automático).</param>
    public CloudinaryUploadException(string message) 
        : base($"Error al subir imagen a Cloudinary: {message}") { }
    
    /// <summary>Crea una excepción de error de subida con mensaje y excepción interna.</summary>
    /// <param name="message">Detalle del error de subida.</param>
    /// <param name="innerException">Excepción original lanzada por CloudinaryDotNet.</param>
    public CloudinaryUploadException(string message, Exception innerException) 
        : base($"Error al subir imagen a Cloudinary: {message}", innerException) { }
}

/// <summary>
/// Excepción que se produce cuando falla la eliminación de una imagen en Cloudinary.
/// </summary>
/// <remarks>
/// <para>Incluye automáticamente el prefijo "Error al eliminar imagen de Cloudinary: "
/// en el mensaje, concatenándolo con el mensaje específico recibido.</para>
/// <para><b>Escenarios típicos:</b></para>
/// <list type="bullet">
///   <item><description>El publicId de la imagen no existe en Cloudinary.</description></item>
///   <item><description>Error de conexión con la API de Cloudinary.</description></item>
///   <item><description>La imagen está siendo utilizada y no puede eliminarse.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 422 Unprocessable Entity
/// con errorType "CloudinaryDeleteError".</para>
/// </remarks>
public class CloudinaryDeleteException : CloudinaryException
{
    /// <summary>Crea una excepción de error de eliminación con mensaje específico.</summary>
    /// <param name="message">Detalle del error de eliminación (se concatena al prefijo automático).</param>
    public CloudinaryDeleteException(string message) 
        : base($"Error al eliminar imagen de Cloudinary: {message}") { }
    
    /// <summary>Crea una excepción de error de eliminación con mensaje y excepción interna.</summary>
    /// <param name="message">Detalle del error de eliminación.</param>
    /// <param name="innerException">Excepción original lanzada por CloudinaryDotNet.</param>
    public CloudinaryDeleteException(string message, Exception innerException) 
        : base($"Error al eliminar imagen de Cloudinary: {message}", innerException) { }
}

/// <summary>
/// Excepción que se produce cuando hay un error en la configuración de Cloudinary.
/// </summary>
/// <remarks>
/// <para>Incluye automáticamente el prefijo "Error de configuración de Cloudinary: "
/// en el mensaje, concatenándolo con el mensaje específico recibido.</para>
/// <para><b>Escenarios típicos:</b></para>
/// <list type="bullet">
///   <item><description>Faltan variables de entorno necesarias (CloudName, ApiKey, ApiSecret).</description></item>
///   <item><description>Las credenciales configuradas son inválidas.</description></item>
///   <item><description>El cloud name no existe o está desactivado.</description></item>
///   <item><description>Error al inicializar la cuenta de Cloudinary desde <c>CloudinaryConfig</c> o <c>AppConfigExtensions</c>.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 500 Internal Server Error
/// con errorType "CloudinaryConfigurationError".</para>
/// </remarks>
public class CloudinaryConfigurationException : CloudinaryException
{
    /// <summary>Crea una excepción de configuración con mensaje específico.</summary>
    /// <param name="message">Detalle del error de configuración (se concatena al prefijo automático).</param>
    public CloudinaryConfigurationException(string message) 
        : base($"Error de configuración de Cloudinary: {message}") { }
}

/// <summary>
/// Excepción que se produce cuando se proporciona un parámetro inválido a una operación de Cloudinary.
/// </summary>
/// <remarks>
/// <para>Incluye automáticamente el prefijo "Parámetro inválido para Cloudinary: "
/// en el mensaje, concatenándolo con el mensaje específico recibido.</para>
/// <para><b>Escenarios típicos:</b></para>
/// <list type="bullet">
///   <item><description>URL de imagen vacía o con formato incorrecto.</description></item>
///   <item><description>publicId nulo o vacío al intentar eliminar una imagen.</description></item>
///   <item><description>Parámetros de transformación de imagen inválidos (width, height, crop).</description></item>
///   <item><description>Folder de destino inválido o inexistente.</description></item>
/// </list>
/// <para>Mapeado por <see cref="GlobalExceptionHandler"/> a HTTP 400 Bad Request
/// con errorType "CloudinaryInvalidParameterError".</para>
/// </remarks>
public class CloudinaryInvalidParameterException : CloudinaryException
{
    /// <summary>Crea una excepción de parámetro inválido con mensaje específico.</summary>
    /// <param name="message">Detalle del parámetro inválido (se concatena al prefijo automático).</param>
    public CloudinaryInvalidParameterException(string message) 
        : base($"Parámetro inválido para Cloudinary: {message}") { }
}

