using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestEncordados.Integration.Api;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string[] EnvVars =
    [
        "Cloudinary__CloudName",
        "Cloudinary__ApiKey",
        "Cloudinary__ApiSecret",
        "JWT_KEY"
    ];

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Cloudinary__CloudName", "test-cloud");
        Environment.SetEnvironmentVariable("Cloudinary__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("Cloudinary__ApiSecret", "test-secret");
        Environment.SetEnvironmentVariable("JWT_KEY", "this-is-a-test-key-that-is-long-enough-for-hmac-sha256");
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var key in EnvVars)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
        base.Dispose(disposing);
    }
}
