using BackEncordados.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace TestEncordados.Unit.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
    private readonly RequestDelegate _next;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
        _next = _ => Task.CompletedTask;
        _handler = new GlobalExceptionHandler(_next, _mockLogger.Object);
    }

    private static HttpContext CreateHttpContext(string path = "/api/test", string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Test]
    public async Task InvokeAsync_NoException_StatusCodeStaysAsSet()
    {
        var context = CreateHttpContext();
        var nextCalled = false;
        var next = new RequestDelegate(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new UnauthorizedAccessException("Not authorized"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
        await VerifyResponse(context, "No autorizado", "UnauthorizedError");
    }

    [Test]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new ArgumentException("Invalid argument"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        await VerifyResponse(context, "Invalid argument", "ValidationError");
    }

    [Test]
    public async Task InvokeAsync_CloudinaryUploadException_Returns422()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new BackEncordados.Common.Exceptions.CloudinaryUploadException("Upload failed"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(422);
        await VerifyResponse(context, "Error al subir imagen a Cloudinary: Upload failed", "CloudinaryUploadError");
    }

    [Test]
    public async Task InvokeAsync_CloudinaryDeleteException_Returns422()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new BackEncordados.Common.Exceptions.CloudinaryDeleteException("Delete failed"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(422);
        await VerifyResponse(context, "Error al eliminar imagen de Cloudinary: Delete failed", "CloudinaryDeleteError");
    }

    [Test]
    public async Task InvokeAsync_CloudinaryConfigurationException_Returns500()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new BackEncordados.Common.Exceptions.CloudinaryConfigurationException("Config error"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        await VerifyResponse(context, "Error de configuración de Cloudinary: Config error", "CloudinaryConfigurationError");
    }

    [Test]
    public async Task InvokeAsync_CloudinaryInvalidParameterException_Returns400()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new BackEncordados.Common.Exceptions.CloudinaryInvalidParameterException("Invalid parameter"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        await VerifyResponse(context, "Parámetro inválido para Cloudinary: Invalid parameter", "CloudinaryInvalidParameterError");
    }

    [Test]
    public async Task InvokeAsync_DbUpdateException_Returns409()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new DbUpdateException("Database error"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(409);
        await VerifyResponse(context, "Error al actualizar la base de datos", "ConflictError");
    }

    [Test]
    public async Task InvokeAsync_TimeoutException_Returns408()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new TimeoutException("Timeout"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(408);
        await VerifyResponse(context, "Tiempo de espera agotado", "InternalError");
    }

    [Test]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new InvalidOperationException("Something went wrong"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        await VerifyResponse(context, "Ha ocurrido un error interno", "InternalError");
    }

    [Test]
    public async Task InvokeAsync_ResponseContainsErrorId()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new Exception("Test"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("errorId").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("errorId").GetString().Should().HaveLength(8);
    }

    [Test]
    public async Task InvokeAsync_ResponseContainsPathAndMethod()
    {
        var context = CreateHttpContext("/api/customers", "POST");
        var next = new RequestDelegate(_ => throw new Exception("Test"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("\"/api/customers\"");
        body.Should().Contain("\"POST\"");
    }

    [Test]
    public async Task InvokeAsync_ResponseContainsTimestamp()
    {
        var context = CreateHttpContext();
        var next = new RequestDelegate(_ => throw new Exception("Test"));
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("\"timestamp\"");
    }

    [Test]
    public async Task InvokeAsync_LogsException()
    {
        var context = CreateHttpContext();
        var exception = new Exception("Test error");
        var next = new RequestDelegate(_ => throw exception);
        var handler = new GlobalExceptionHandler(next, _mockLogger.Object);

        await handler.InvokeAsync(context);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static async Task VerifyResponse(HttpContext context, string expectedMessage, string expectedErrorType)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("message").GetString().Should().Be(expectedMessage);
        json.GetProperty("errorType").GetString().Should().Be(expectedErrorType);
        json.GetProperty("errorId").GetString().Should().NotBeNullOrEmpty();
    }
}