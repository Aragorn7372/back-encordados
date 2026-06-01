using BackEncordados.Common.Exceptions;
using BackEncordados.Common.Service.Cloudinary;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Common.Service.Cloudinary;

public class CloudinaryServiceAdditionalTests
{
    private Mock<ILogger<CloudinaryService>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        CloudinaryOptions.Current = null;
        _mockLogger = new Mock<ILogger<CloudinaryService>>();
    }

    [TearDown]
    public void TearDown()
    {
        CloudinaryOptions.Current = null;
    }

    [Test]
    public void GetDefaultImageUrl_WhenOptionsNotInitialized_ThrowsConfigurationException()
    {
        var service = new CloudinaryService(_mockLogger.Object);

        var act = () => service.GetDefaultImageUrl(CloudinaryConstants.FOLDER_USUARIOS);

        act.Should().Throw<CloudinaryConfigurationException>()
            .WithMessage("*CloudinaryOptions no ha sido inicializado*");
    }

    [Test]
    public void GetImageUrlWithTransformations_WhenOptionsNotInitialized_ThrowsConfigurationException()
    {
        var service = new CloudinaryService(_mockLogger.Object);

        var act = () => service.GetImageUrlWithTransformations("test-public-id");

        act.Should().Throw<CloudinaryConfigurationException>()
            .WithMessage("*CloudinaryOptions no ha sido inicializado*");
    }

    [Test]
    public void GetImageUrl_WhenOptionsInitialized_ReturnsUrl()
    {
        CloudinaryOptions.Current = CreateValidOptions();
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.GetImageUrl("test-file", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("res.cloudinary.com");
    }

    [Test]
    public void GetDefaultImageUrl_WithMateriesFolder_ReturnsDefaultMateries()
    {
        CloudinaryOptions.Current = new CloudinaryOptions
        {
            CloudName = "testcloud",
            ApiKey = "key",
            ApiSecret = "secret",
            DefaultImages = new DefaultImageOptions
            {
                Materies = "default_material"
            }
        };
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.GetDefaultImageUrl(CloudinaryConstants.FOLDER_MATERIES);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_material");
    }

    [Test]
    public void ResolveImageUrl_WhenStartsWithHttp_ReturnsAsIs()
    {
        CloudinaryOptions.Current = CreateValidOptions();
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.ResolveImageUrl("http://example.com/img.png", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Be("http://example.com/img.png");
    }

    [Test]
    public void ResolveImageUrl_WhenStartsWithHttps_ReturnsAsIs()
    {
        CloudinaryOptions.Current = CreateValidOptions();
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.ResolveImageUrl("https://cdn.example.com/photo.jpg", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Be("https://cdn.example.com/photo.jpg");
    }

    [Test]
    public void ResolveImageUrl_WhenPublicId_ReturnsTransformedUrl()
    {
        CloudinaryOptions.Current = CreateValidOptions();
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.ResolveImageUrl("usuarios/photo123", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("photo123");
    }

    [Test]
    public void GetImageUrlWithTransformations_WithCustomDimensions_UsesCustomValues()
    {
        CloudinaryOptions.Current = CreateValidOptions();
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.GetImageUrlWithTransformations("test/public-id", 300, 400);

        result.Should().Contain("w_300");
        result.Should().Contain("h_400");
    }

    [Test]
    public void GetImageUrlWithTransformations_WithNullWidth_UsesDefault()
    {
        CloudinaryOptions.Current = new CloudinaryOptions
        {
            CloudName = "testcloud",
            ApiKey = "key",
            ApiSecret = "secret",
            Transformations = new TransformationOptions
            {
                Width = 500,
                Height = 500
            }
        };
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.GetImageUrlWithTransformations("test/public-id", null, 300);

        result.Should().Contain("w_500");
        result.Should().Contain("h_300");
    }

    [Test]
    public void GetImageUrlWithTransformations_WithNullHeight_UsesDefault()
    {
        CloudinaryOptions.Current = new CloudinaryOptions
        {
            CloudName = "testcloud",
            ApiKey = "key",
            ApiSecret = "secret",
            Transformations = new TransformationOptions
            {
                Width = 500,
                Height = 500
            }
        };
        var service = new CloudinaryService(_mockLogger.Object);

        var result = service.GetImageUrlWithTransformations("test/public-id", 200, null);

        result.Should().Contain("w_200");
        result.Should().Contain("h_500");
    }

    private static CloudinaryOptions CreateValidOptions()
    {
        return new CloudinaryOptions
        {
            CloudName = "testcloud",
            ApiKey = "testkey",
            ApiSecret = "testsecret",
            Transformations = new TransformationOptions
            {
                Width = 800,
                Height = 600,
                Crop = "fill",
                Quality = "auto"
            },
            DefaultImages = new DefaultImageOptions
            {
                Usuarios = "default_user",
                Talleres = "default_taller",
                Materies = "default_material"
            }
        };
    }
}
