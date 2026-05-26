using BackEncordados.Common.Service.Cloudinary;
using Microsoft.AspNetCore.Http;
using Moq;

namespace TestEncordados.Unit.Fixtures;

public static class CloudinaryServiceBuilder
{
    public static Mock<ICloudinaryService> Create()
    {
        var mock = new Mock<ICloudinaryService>();

        mock.Setup(c => c.GetDefaultImageUrl(It.IsAny<string>()))
            .Returns("https://res.cloudinary.com/test/image/upload/v1/defaults/default.png");

        mock.Setup(c => c.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://res.cloudinary.com/test/image/upload/v1/uploads/test_id");

        mock.Setup(c => c.UploadWithAutoNameAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new CloudinaryUploadResult
            {
                PublicId = "test_public_id",
                ImageUrl = "https://res.cloudinary.com/test/image/upload/v1/uploads/test_id",
                GeneratedFilename = "test_filename"
            });

        mock.Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(c => c.GetImageUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://res.cloudinary.com/test/image/upload/v1/uploads/test_url");

        return mock;
    }
}