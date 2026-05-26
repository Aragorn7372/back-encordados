using FluentAssertions;

namespace TestEncordados.Integration.Api;

public class AppStartupTests
{
    [Test]
    public async Task Application_StartsSuccessfully()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
