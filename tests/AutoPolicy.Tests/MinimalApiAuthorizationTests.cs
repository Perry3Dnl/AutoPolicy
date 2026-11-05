using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class MinimalApiAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MinimalApiAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MinimalApiEndpoints_AreDiscoveredAndDeniedWithoutMatchingPermission()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/analytics")).StatusCode);

        using var allowedRoutePermission = new HttpRequestMessage(HttpMethod.Get, "/api/analytics");
        allowedRoutePermission.Headers.Add("X-AutoPolicy-Allow", "/api/analytics");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowedRoutePermission)).StatusCode);

        using var wrongPermission = new HttpRequestMessage(HttpMethod.Post, "/api/packages/upload");
        wrongPermission.Headers.Add("X-AutoPolicy-Allow", "/api/packages/upload");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(wrongPermission)).StatusCode);

        using var allowedExplicitPermission = new HttpRequestMessage(HttpMethod.Post, "/api/packages/upload");
        allowedExplicitPermission.Headers.Add("X-AutoPolicy-Allow", "Dashboard.Packages.Upload");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowedExplicitPermission)).StatusCode);

        var registry = _factory.Services.GetRequiredService<IPermissionRegistry>();
        Assert.Contains("/api/analytics", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/Dashboard.Packages.Upload", registry.Keys, StringComparer.OrdinalIgnoreCase);
    }
}
