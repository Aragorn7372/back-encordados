using BackEncordados.Common.Exceptions;
using FluentAssertions;

namespace TestEncordados.Unit.Common.Exceptions;

public class CloudinaryExceptionTests
{
    #region CloudinaryException Tests

    [Test]
    public void CloudinaryException_InheritsFromException()
    {
        var exception = new CloudinaryException("Test message");

        exception.Should().BeAssignableTo<Exception>();
    }

    [Test]
    public void CloudinaryException_ConstructorWithMessage_SetsMessage()
    {
        var exception = new CloudinaryException("Test message");

        exception.Message.Should().Be("Test message");
    }

    [Test]
    public void CloudinaryException_ConstructorWithInnerException_SetsBoth()
    {
        var innerException = new InvalidOperationException("Inner message");
        var exception = new CloudinaryException("Outer message", innerException);

        exception.Message.Should().Be("Outer message");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region CloudinaryUploadException Tests

    [Test]
    public void CloudinaryUploadException_InheritsFromCloudinaryException()
    {
        var exception = new CloudinaryUploadException("test error");

        exception.Should().BeAssignableTo<CloudinaryException>();
    }

    [Test]
    public void CloudinaryUploadException_ConstructorWithMessage_PrefixesMessage()
    {
        var exception = new CloudinaryUploadException("test error");

        exception.Message.Should().Contain("Error al subir imagen a Cloudinary:");
        exception.Message.Should().Contain("test error");
    }

    [Test]
    public void CloudinaryUploadException_ConstructorWithInnerException_SetsBoth()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new CloudinaryUploadException("upload error", innerException);

        exception.Message.Should().Contain("Error al subir imagen a Cloudinary:");
        exception.Message.Should().Contain("upload error");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region CloudinaryDeleteException Tests

    [Test]
    public void CloudinaryDeleteException_InheritsFromCloudinaryException()
    {
        var exception = new CloudinaryDeleteException("test error");

        exception.Should().BeAssignableTo<CloudinaryException>();
    }

    [Test]
    public void CloudinaryDeleteException_ConstructorWithMessage_PrefixesMessage()
    {
        var exception = new CloudinaryDeleteException("test error");

        exception.Message.Should().Contain("Error al eliminar imagen de Cloudinary:");
        exception.Message.Should().Contain("test error");
    }

    [Test]
    public void CloudinaryDeleteException_ConstructorWithInnerException_SetsBoth()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new CloudinaryDeleteException("delete error", innerException);

        exception.Message.Should().Contain("Error al eliminar imagen de Cloudinary:");
        exception.Message.Should().Contain("delete error");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region CloudinaryConfigurationException Tests

    [Test]
    public void CloudinaryConfigurationException_InheritsFromCloudinaryException()
    {
        var exception = new CloudinaryConfigurationException("test error");

        exception.Should().BeAssignableTo<CloudinaryException>();
    }

    [Test]
    public void CloudinaryConfigurationException_ConstructorWithMessage_PrefixesMessage()
    {
        var exception = new CloudinaryConfigurationException("invalid credentials");

        exception.Message.Should().Contain("Error de configuración de Cloudinary:");
        exception.Message.Should().Contain("invalid credentials");
    }

    #endregion

    #region CloudinaryInvalidParameterException Tests

    [Test]
    public void CloudinaryInvalidParameterException_InheritsFromCloudinaryException()
    {
        var exception = new CloudinaryInvalidParameterException("test error");

        exception.Should().BeAssignableTo<CloudinaryException>();
    }

    [Test]
    public void CloudinaryInvalidParameterException_ConstructorWithMessage_PrefixesMessage()
    {
        var exception = new CloudinaryInvalidParameterException("public_id");

        exception.Message.Should().Contain("Parámetro inválido para Cloudinary:");
        exception.Message.Should().Contain("public_id");
    }

    #endregion

    #region Additional Edge Cases

    [Test]
    public void AllCloudinaryExceptions_CanBeCaughtAsBaseException()
    {
        Action catchCloudinary = () => throw new CloudinaryException("test");
        Action catchUpload = () => throw new CloudinaryUploadException("test");
        Action catchDelete = () => throw new CloudinaryDeleteException("test");
        Action catchConfig = () => throw new CloudinaryConfigurationException("test");
        Action catchInvalidParam = () => throw new CloudinaryInvalidParameterException("test");

        catchCloudinary.Should().Throw<CloudinaryException>();
        catchUpload.Should().Throw<CloudinaryException>();
        catchDelete.Should().Throw<CloudinaryException>();
        catchConfig.Should().Throw<CloudinaryException>();
        catchInvalidParam.Should().Throw<CloudinaryException>();
    }

    [Test]
    public void UploadException_MessageFormat_IsConsistent()
    {
        var exception1 = new CloudinaryUploadException("file not found");
        var exception2 = new CloudinaryUploadException("timeout");

        exception1.Message.Should().StartWith("Error al subir imagen a Cloudinary:");
        exception2.Message.Should().StartWith("Error al subir imagen a Cloudinary:");
    }

    [Test]
    public void DeleteException_MessageFormat_IsConsistent()
    {
        var exception1 = new CloudinaryDeleteException("file not found");
        var exception2 = new CloudinaryDeleteException("timeout");

        exception1.Message.Should().StartWith("Error al eliminar imagen de Cloudinary:");
        exception2.Message.Should().StartWith("Error al eliminar imagen de Cloudinary:");
    }

    [Test]
    public void ConfigurationException_MessageFormat_IsConsistent()
    {
        var exception1 = new CloudinaryConfigurationException("missing api key");
        var exception2 = new CloudinaryConfigurationException("invalid secret");

        exception1.Message.Should().StartWith("Error de configuración de Cloudinary:");
        exception2.Message.Should().StartWith("Error de configuración de Cloudinary:");
    }

    [Test]
    public void InvalidParameterException_MessageFormat_IsConsistent()
    {
        var exception1 = new CloudinaryInvalidParameterException("empty public_id");
        var exception2 = new CloudinaryInvalidParameterException("invalid format");

        exception1.Message.Should().StartWith("Parámetro inválido para Cloudinary:");
        exception2.Message.Should().StartWith("Parámetro inválido para Cloudinary:");
    }

    [Test]
    public void InnerException_PropagatesFullStackTrace()
    {
        var originalException = new DivideByZeroException("original");
        var cloudinaryException = new CloudinaryUploadException("upload failed", originalException);

        cloudinaryException.InnerException.Should().NotBeNull();
        cloudinaryException.InnerException!.Message.Should().Be("original");
        cloudinaryException.Message.Should().Contain("upload failed");
    }

    #endregion
}