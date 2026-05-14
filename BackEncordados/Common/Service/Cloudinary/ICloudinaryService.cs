namespace BackEncordados.Common.Service.Cloudinary;


public interface ICloudinaryService
{

    string GetImageUrl(string filename, string folder);
    string GetDefaultImageUrl(string folder);
    Task<string> UploadAsync(IFormFile file, string filename, string folder);
    Task<CloudinaryUploadResult> UploadWithAutoNameAsync(IFormFile file, string entityId, string folder);
    Task DeleteAsync(string publicId);
    string GetImageUrlWithTransformations(string publicId, int? width = null, int? height = null);
    string ResolveImageUrl(string storedValue, string folder);
}


public class CloudinaryUploadResult
{
    public string PublicId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string GeneratedFilename { get; set; } = string.Empty;
}

