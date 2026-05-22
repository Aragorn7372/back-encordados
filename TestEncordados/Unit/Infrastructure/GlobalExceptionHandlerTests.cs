using System.Text.Json;
using BackEncordados.Common.Exceptions;
using BackEncordados.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace TestEncordados.Unit.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static GlobalExceptionHandler CreateHandler(Func<HttpContext, Task> nextAction)
    {
        var next = new RequestDelegate(nextAction);
        var logger = Mock.Of<ILogger<GlobalExceptionHandler>>();
        return new GlobalExceptionHandler(next, logger);
    }

    private static async Task<HttpContext> ExecuteWithException(Exception ex)
    {
        var handler = CreateHandler(_ => throw ex);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/test";
        context.Request.Method = "GET";

        await handler.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return context;
    }

    private static async Task<JsonDocument> GetResponseBody(HttpContext context)
    {
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonDocument.Parse(body);
    }

    [Test]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var context = await ExecuteWithException(new UnauthorizedAccessException());

        context.Response.StatusCode.Should().Be(401);
    }

    [Test]
    public async Task ArgumentException_Returns400()
    {
        var context = await ExecuteWithException(new ArgumentException("Bad arg"));

        context.Response.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task DbUpdateException_Returns409()
    {
        var context = await ExecuteWithException(new DbUpdateException("DB error"));

        context.Response.StatusCode.Should().Be(409);
    }

    [Test]
    public async Task TimeoutException_Returns408()
    {
        var context = await ExecuteWithException(new TimeoutException("Timeout"));

        context.Response.StatusCode.Should().Be(408);
    }

    [Test]
    public async Task CloudinaryUploadException_Returns422()
    {
        var context = await ExecuteWithException(new CloudinaryUploadException("Upload failed"));

        context.Response.StatusCode.Should().Be(422);
    }

    [Test]
    public async Task CloudinaryDeleteException_Returns422()
    {
        var context = await ExecuteWithException(new CloudinaryDeleteException("Delete failed"));

        context.Response.StatusCode.Should().Be(422);
    }

    [Test]
    public async Task CloudinaryConfigurationException_Returns500()
    {
        var context = await ExecuteWithException(new CloudinaryConfigurationException("Config error"));

        context.Response.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task CloudinaryInvalidParameterException_Returns400()
    {
        var context = await ExecuteWithException(new CloudinaryInvalidParameterException("Bad param"));

        context.Response.StatusCode.Should().Be(400);
    }

    [Test]
    public async Task GenericException_Returns500()
    {
        var context = await ExecuteWithException(new InvalidOperationException("Something broke"));

        context.Response.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task ResponseBody_HasRequiredFields()
    {
        var context = await ExecuteWithException(new UnauthorizedAccessException());
        var doc = await GetResponseBody(context);

        doc.RootElement.TryGetProperty("errorId", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("message", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("errorType", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("path", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("method", out _).Should().BeTrue();
    }

    [Test]
    public async Task ResponseBody_Unauthorized_HasCorrectErrorType()
    {
        var context = await ExecuteWithException(new UnauthorizedAccessException());
        var doc = await GetResponseBody(context);

        var errorType = doc.RootElement.GetProperty("errorType").GetString();
        errorType.Should().Be("UnauthorizedError");
    }

    [Test]
    public async Task ResponseBody_ArgumentException_HasCorrectErrorType()
    {
        var context = await ExecuteWithException(new ArgumentException("test"));
        var doc = await GetResponseBody(context);

        var errorType = doc.RootElement.GetProperty("errorType").GetString();
        errorType.Should().Be("ValidationError");
    }

    [Test]
    public async Task NoException_PassesThrough()
    {
        var called = false;
        var handler = CreateHandler(ctx =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await handler.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }
}
