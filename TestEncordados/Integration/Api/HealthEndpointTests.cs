using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TestEncordados.Integration.Api;

public class HealthEndpointTests
{
    [Test]
    public async Task GetHealth_ReturnsOkWithStatusHealthy()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("status");
        body!["status"]?.ToString().Should().Be("healthy");
        body.Should().ContainKey("timestamp");
    }

    [Test]
    public async Task GetHealth_ReturnsJsonContentType()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task GetNonExistentEndpoint_ReturnsNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/nonexistent-route");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
