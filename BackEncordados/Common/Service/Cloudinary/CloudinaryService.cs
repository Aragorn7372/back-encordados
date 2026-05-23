using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BackEncordados.Common.Exceptions;

namespace BackEncordados.Common.Service.Cloudinary;


public class CloudinaryService(ILogger<CloudinaryService> logger) : ICloudinaryService
{
    private static CloudinaryDotNet.Cloudinary? _cloudinary;

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
    
    public string GetImageUrl(string filename, string folder)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return GetDefaultImageUrl(folder);
        }

        var publicId = $"{folder}/{filename}";
        return BuildTransformedUrl(publicId);
    }

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
    
    private static string GenerateFilename(string entityId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{entityId}_{timestamp}";
    }

    private string BuildTransformedUrl(string publicId)
    {
        return GetImageUrlWithTransformations(publicId);
    }
    
    private static void ValidateFolder(string folder)
    {
        var validFolders = new[] { CloudinaryConstants.FOLDER_USUARIOS, CloudinaryConstants.FOLDER_TALLERES, CloudinaryConstants.FOLDER_MATERIES };
        if (!validFolders.Contains(folder.ToLower()))
        {
            throw new CloudinaryInvalidParameterException($"Carpeta no válida. Debe ser: {string.Join(", ", validFolders)}");
        }
    }
}


public class CloudinaryOptions {

    public static CloudinaryOptions? Current { get; set; }

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public TransformationOptions Transformations { get; set; } = new();
    public DefaultImageOptions DefaultImages { get; set; } = new();
}


public class TransformationOptions {
    public int Width { get; set; } = CloudinaryConstants.DEFAULT_WIDTH;
    public int Height { get; set; } = CloudinaryConstants.DEFAULT_HEIGHT;
    public string Crop { get; set; } = CloudinaryConstants.DEFAULT_CROP;
    public string Quality { get; set; } = CloudinaryConstants.DEFAULT_QUALITY;
    public string Format { get; set; } = CloudinaryConstants.DEFAULT_FORMAT;
}

public class DefaultImageOptions
{
    public string Usuarios { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_USUARIOS;
    public string Talleres { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_TALLERES;
    public string Materies { get; set; } = CloudinaryConstants.DEFAULT_IMAGE_MATERIALES;
}

