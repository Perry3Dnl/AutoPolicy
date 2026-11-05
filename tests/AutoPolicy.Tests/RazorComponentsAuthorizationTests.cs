using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class RazorComponentsAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RazorComponentsAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StaticSsrComponentEndpoint_IsProtectedByDefault()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/components/probe")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/components/probe");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/components/probe");
        var response = await client.SendAsync(allowed);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Razor Component endpoint", await response.Content.ReadAsStringAsync());
    }
}
