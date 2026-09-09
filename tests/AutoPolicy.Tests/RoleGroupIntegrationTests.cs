using System.Net;
using AutoPolicy.TestApp.Pages;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class RoleGroupIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RoleGroupIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllowedRole_ExpandsNestedGroupsThroughRealAuthorizationPipeline()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        request.Headers.Add("X-AutoPolicy-Allow-Role", "Staff");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, DiscoveryExecutionProbe.Count);
    }

    [Fact]
    public async Task DeniedGroup_OverridesAllowedRoleAndPreventsHandlerExecution()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        request.Headers.Add("X-AutoPolicy-Allow-Role", "Staff");
        request.Headers.Add("X-AutoPolicy-Deny-Group", "ProbeAccess");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, DiscoveryExecutionProbe.Count);
    }

    [Fact]
    public async Task DeniedGroup_RemovesOnlyItsOwnCapabilityFromAllowedRole()
    {
        using var client = _factory.CreateClient();

        using var probe = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        probe.Headers.Add("X-AutoPolicy-Allow-Role", "Staff");
        probe.Headers.Add("X-AutoPolicy-Deny-Group", "ProbeAccess");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(probe)).StatusCode);

        using var nested = new HttpRequestMessage(HttpMethod.Get, "/Nested/Detail");
        nested.Headers.Add("X-AutoPolicy-Allow-Role", "Staff");
        nested.Headers.Add("X-AutoPolicy-Deny-Group", "ProbeAccess");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(nested)).StatusCode);
    }

    [Fact]
    public async Task DeniedRole_OverridesDirectPermissionGrant()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        request.Headers.Add("X-AutoPolicy-Allow", "/Probe");
        request.Headers.Add("X-AutoPolicy-Deny-Role", "Staff");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, DiscoveryExecutionProbe.Count);
    }

    [Fact]
    public async Task DirectDeny_OverridesMatchAllAdministratorRole()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        request.Headers.Add("X-AutoPolicy-Allow-Role", "Administrator");
        request.Headers.Add("X-AutoPolicy-Deny", "/Probe");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, DiscoveryExecutionProbe.Count);
    }

    [Fact]
    public async Task UnknownAllowedRole_FailsClosed()
    {
        DiscoveryExecutionProbe.Reset();
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Probe");
        request.Headers.Add("X-AutoPolicy-Allow-Role", "MissingRole");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, DiscoveryExecutionProbe.Count);
    }
}
