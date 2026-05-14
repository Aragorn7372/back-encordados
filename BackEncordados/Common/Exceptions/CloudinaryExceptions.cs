namespace BackEncordados.Common.Exceptions;


public class CloudinaryException : Exception
{
    public CloudinaryException(string message) : base(message) { }
    public CloudinaryException(string message, Exception innerException) 
        : base(message, innerException) { }
}
public class CloudinaryUploadException : CloudinaryException
{
    public CloudinaryUploadException(string message) 
        : base($"Error al subir imagen a Cloudinary: {message}") { }
    
    public CloudinaryUploadException(string message, Exception innerException) 
        : base($"Error al subir imagen a Cloudinary: {message}", innerException) { }
}


public class CloudinaryDeleteException : CloudinaryException
{
    public CloudinaryDeleteException(string message) 
        : base($"Error al eliminar imagen de Cloudinary: {message}") { }
    
    public CloudinaryDeleteException(string message, Exception innerException) 
        : base($"Error al eliminar imagen de Cloudinary: {message}", innerException) { }
}


public class CloudinaryConfigurationException : CloudinaryException
{
    public CloudinaryConfigurationException(string message) 
        : base($"Error de configuración de Cloudinary: {message}") { }
}

public class CloudinaryInvalidParameterException : CloudinaryException
{
    public CloudinaryInvalidParameterException(string message) 
        : base($"Parámetro inválido para Cloudinary: {message}") { }
}

