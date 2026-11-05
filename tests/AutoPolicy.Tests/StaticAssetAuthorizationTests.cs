using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class StaticAssetAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StaticAssetAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MapStaticAssets_AreProtectedByDefault()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync("/auto-policy-probe.txt")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/auto-policy-probe.txt");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/auto-policy-probe.txt");
        var response = await client.SendAsync(allowed);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("static asset probe", await response.Content.ReadAsStringAsync());
    }
}
