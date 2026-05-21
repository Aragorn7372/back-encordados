using BackEncordados.Common.Exceptions;
using BackEncordados.Common.Service.Cloudinary;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Common.Service.Cloudinary;

public class CloudinaryServiceTests
{
    private Mock<ILogger<CloudinaryService>> _mockLogger = null!;
    private CloudinaryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        CloudinaryOptions.Current = new CloudinaryOptions
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
                Talleres = "default_taller"
            }
        };

        _mockLogger = new Mock<ILogger<CloudinaryService>>();
        _service = new CloudinaryService(_mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        CloudinaryOptions.Current = null;
    }

    private static Mock<IFormFile> CreateValidFileMock()
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.Length).Returns(1);
        mock.Setup(f => f.FileName).Returns("test.jpg");
        return mock;
    }

    [Test]
    public void GetImageUrl_WhenFilenameEmpty_ReturnsDefault()
    {
        var result = _service.GetImageUrl("", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_user");
    }

    [Test]
    public void GetImageUrl_WhenFilenameNull_ReturnsDefault()
    {
        var result = _service.GetImageUrl(null!, CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_user");
    }

    [Test]
    public void GetImageUrl_WhenFilenameValid_BuildsUrl()
    {
        var result = _service.GetImageUrl("avatar123", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("usuarios/avatar123");
    }

    [Test]
    public void GetDefaultImageUrl_WhenFolderUsuarios_ReturnsDefaultUsuario()
    {
        var result = _service.GetDefaultImageUrl(CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_user");
    }

    [Test]
    public void GetDefaultImageUrl_WhenFolderTalleres_ReturnsDefaultTaller()
    {
        var result = _service.GetDefaultImageUrl(CloudinaryConstants.FOLDER_TALLERES);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_taller");
    }

    [Test]
    public void GetDefaultImageUrl_WhenFolderInvalid_Throws()
    {
        var act = () => _service.GetDefaultImageUrl("invalida");

        act.Should().Throw<CloudinaryInvalidParameterException>()
            .WithMessage("*Carpeta no válida*");
    }

    [Test]
    public void GetImageUrlWithTransformations_WhenValid_BuildsUrl()
    {
        var result = _service.GetImageUrlWithTransformations("usuarios/test123");

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("test123");
        result.Should().Contain("w_800");
        result.Should().Contain("h_600");
    }

    [Test]
    public void GetImageUrlWithTransformations_WithCustomDimensions_BuildsUrl()
    {
        var result = _service.GetImageUrlWithTransformations("talleres/photo", 200, 200);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("w_200");
        result.Should().Contain("h_200");
    }

    [Test]
    public void GetImageUrlWithTransformations_WhenPublicIdEmpty_Throws()
    {
        var act = () => _service.GetImageUrlWithTransformations("");

        act.Should().Throw<CloudinaryInvalidParameterException>()
            .WithMessage("*public ID es requerido*");
    }

    [Test]
    public void ResolveImageUrl_WhenNull_ReturnsDefault()
    {
        var result = _service.ResolveImageUrl(null!, CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_user");
    }

    [Test]
    public void ResolveImageUrl_WhenEmpty_ReturnsDefault()
    {
        var result = _service.ResolveImageUrl("", CloudinaryConstants.FOLDER_TALLERES);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("default_taller");
    }

    [Test]
    public void ResolveImageUrl_WhenStartsWithHttp_ReturnsAsIs()
    {
        var result = _service.ResolveImageUrl("https://example.com/img.png", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Be("https://example.com/img.png");
    }

    [Test]
    public void ResolveImageUrl_WhenPublicId_ReturnsTransformedUrl()
    {
        var result = _service.ResolveImageUrl("usuarios/myphoto", CloudinaryConstants.FOLDER_USUARIOS);

        result.Should().Contain("res.cloudinary.com");
        result.Should().Contain("myphoto");
    }

    [Test]
    public async Task UploadAsync_WhenFileNull_Throws()
    {
        var act = async () => await _service.UploadAsync(null!, "test", CloudinaryConstants.FOLDER_USUARIOS);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*archivo es requerido*");
    }

    [Test]
    public async Task UploadAsync_WhenFileEmpty_Throws()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var act = async () => await _service.UploadAsync(fileMock.Object, "test", CloudinaryConstants.FOLDER_USUARIOS);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*archivo es requerido*");
    }

    [Test]
    public async Task UploadAsync_WhenFilenameEmpty_Throws()
    {
        var fileMock = CreateValidFileMock();

        var act = async () => await _service.UploadAsync(fileMock.Object, "", CloudinaryConstants.FOLDER_USUARIOS);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*nombre de archivo es requerido*");
    }

    [Test]
    public async Task UploadAsync_WhenFolderInvalid_Throws()
    {
        var fileMock = CreateValidFileMock();

        var act = async () => await _service.UploadAsync(fileMock.Object, "test", "invalida");

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*Carpeta no válida*");
    }

    [Test]
    public async Task DeleteAsync_WhenPublicIdEmpty_Throws()
    {
        var act = async () => await _service.DeleteAsync("");

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*public ID es requerido*");
    }

    [Test]
    public async Task DeleteAsync_WhenPublicIdNull_Throws()
    {
        var act = async () => await _service.DeleteAsync(null!);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*public ID es requerido*");
    }

    [Test]
    public async Task UploadWithAutoNameAsync_WhenFileNull_Throws()
    {
        var act = async () => await _service.UploadWithAutoNameAsync(null!, "entity1", CloudinaryConstants.FOLDER_USUARIOS);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*archivo es requerido*");
    }

    [Test]
    public async Task UploadWithAutoNameAsync_WhenEntityIdEmpty_Throws()
    {
        var fileMock = CreateValidFileMock();

        var act = async () => await _service.UploadWithAutoNameAsync(fileMock.Object, "", CloudinaryConstants.FOLDER_USUARIOS);

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*ID de entidad es requerido*");
    }

    [Test]
    public async Task UploadWithAutoNameAsync_WhenFolderInvalid_Throws()
    {
        var fileMock = CreateValidFileMock();

        var act = async () => await _service.UploadWithAutoNameAsync(fileMock.Object, "entity1", "invalida");

        await act.Should().ThrowAsync<CloudinaryInvalidParameterException>()
            .WithMessage("*Carpeta no válida*");
    }
}
