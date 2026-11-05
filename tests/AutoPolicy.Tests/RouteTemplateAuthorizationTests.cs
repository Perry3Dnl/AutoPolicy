using System.Net;
using AutoPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class RouteTemplateAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RouteTemplateAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnmarkedEndpoints_AreDiscoveredAndRegisteredWhenAuthorized()
    {
        using var client = _factory.CreateClient();
        var registry = _factory.Services.GetRequiredService<IPermissionRegistry>();

        Assert.DoesNotContain("/api/default-deny", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/default-deny")).StatusCode);
        Assert.Contains("/api/default-deny", registry.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RootPath_IsProtectedAndUsesSlashPermission()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowed)).StatusCode);
    }

    [Fact]
    public async Task OptionalRouteTemplate_UsesStablePermissionKey()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/optional/42")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/optional/42");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/api/optional/{id}");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowed)).StatusCode);
    }

    [Fact]
    public async Task CatchAllRouteTemplate_UsesStablePermissionKey()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/files/one/two")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/files/one/two");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/api/files/{path}");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowed)).StatusCode);
    }
}
