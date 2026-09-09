using System.Net;
using AutoPolicy.TestApp.Pages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class DiscoveryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DiscoveryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RazorPages_AreProtectedByDefault_AndHandlersDoNotRunWhenDenied()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();

        var denied = await client.GetAsync("/Probe");

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(0, DiscoveryExecutionProbe.Count);

        using var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        allowedRequest.Headers.Add("X-AutoPolicy-Allow", "/Probe");
        var allowed = await client.SendAsync(allowedRequest);

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(1, DiscoveryExecutionProbe.Count);
    }

    [Fact]
    public async Task Registry_UsesPhysicalRazorPageIdentity_ForNestedIndexCustomRouteAndAreaPages()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-AutoPolicy-Allow", "*");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var registry = _factory.Services.GetRequiredService<IPermissionRegistry>();

        Assert.Contains("/Index", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/Nested/Detail", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/CustomRoute", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/BackOffice/Dashboard", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("/pretty/{id:int}", registry.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomRouteValuesAndQueryStrings_DoNotChangePermissionIdentity()
    {
        using var client = _factory.CreateClient();

        foreach (var url in new[] { "/pretty/12", "/pretty/999?mode=full" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-AutoPolicy-Allow", "/CustomRoute");
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var wrongIdentity = new HttpRequestMessage(HttpMethod.Get, "/pretty/12");
        wrongIdentity.Headers.Add("X-AutoPolicy-Allow", "/pretty/*");
        var denied = await client.SendAsync(wrongIdentity);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var nested = new HttpRequestMessage(HttpMethod.Get, "/Nested/Detail?tab=history");
        nested.Headers.Add("X-AutoPolicy-Allow", "/Nested/Detail");
        var nestedResponse = await client.SendAsync(nested);
        Assert.Equal(HttpStatusCode.OK, nestedResponse.StatusCode);
    }

    [Fact]
    public async Task ScopedAnonymousConventionAndStandardAllowAnonymous_ArePublicWithoutBroadeningNeighbors()
    {
        using var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Public/Info")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/StandardAnonymous")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Publicity/Secret")).StatusCode);
    }

    [Fact]
    public async Task PermissionOverride_ReplacesPhysicalKeyWithoutTrustingTheOriginalKey()
    {
        using var client = _factory.CreateClient();

        using var original = new HttpRequestMessage(HttpMethod.Get, "/Remapped");
        original.Headers.Add("X-AutoPolicy-Allow", "/Remapped");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(original)).StatusCode);

        using var remapped = new HttpRequestMessage(HttpMethod.Get, "/Remapped");
        remapped.Headers.Add("X-AutoPolicy-Allow", "/Virtual/Remapped");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(remapped)).StatusCode);

        var registry = _factory.Services.GetRequiredService<IPermissionRegistry>();
        Assert.Contains("/Virtual/Remapped", registry.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Remapped", registry.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AreaPage_RequiresAreaQualifiedCanonicalPermission()
    {
        using var client = _factory.CreateClient();

        using var wrong = new HttpRequestMessage(HttpMethod.Get, "/BackOffice/Dashboard");
        wrong.Headers.Add("X-AutoPolicy-Allow", "/Dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(wrong)).StatusCode);

        using var right = new HttpRequestMessage(HttpMethod.Get, "/BackOffice/Dashboard");
        right.Headers.Add("X-AutoPolicy-Allow", "/BackOffice/Dashboard");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(right)).StatusCode);
    }

    [Fact]
    public async Task GlobalProtectionCanBeDisabled_WhileAttributeStillOptsInSpecificPage()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                    options.ProtectRazorPagesByDefault(false)));
        });
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Nested/Detail")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/OptIn")).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/OptIn");
        allowed.Headers.Add("X-AutoPolicy-Allow", "/OptIn");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(allowed)).StatusCode);
    }

    [Fact]
    public async Task DuplicateCanonicalMappings_FailApplicationStartup()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                {
                    options.OverridePermissionKey("/Index", "/Collision");
                    options.OverridePermissionKey("/Nested/Detail", "/Collision");
                }));
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = factory.CreateClient();
            _ = await client.GetAsync("/");
        });

        Assert.Contains("Duplicate canonical permission key", exception.ToString());
    }
}
