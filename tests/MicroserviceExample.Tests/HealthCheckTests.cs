using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MicroserviceExample.Tests;

// Boots the real app in-memory. A dummy connection string lets startup succeed;
// /healthz is a pure liveness probe and never touches the database.
public class TestAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting(
            "ConnectionStrings:Default",
            "Host=localhost;Database=test;Username=test;Password=test");
}

public class HealthCheckTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Healthz_ReturnsOk_WithoutDatabase()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
