using AutoPolicy.TestApp.Grpc;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class GrpcAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GrpcAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GrpcCalls_AreDeniedByDefaultAndAllowRoutePermission()
    {
        using var channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions
            {
                HttpHandler = _factory.Server.CreateHandler()
            });
        var client = new Probe.ProbeClient(channel);

        var denied = await Assert.ThrowsAsync<RpcException>(
            async () => await client.GetAsync(new ProbeRequest { Value = "denied" }));
        Assert.Equal(StatusCode.PermissionDenied, denied.StatusCode);

        var headers = new Metadata
        {
            { "x-autopolicy-allow", "/test.Probe/Get" }
        };
        var allowed = await client.GetAsync(
            new ProbeRequest { Value = "allowed" },
            headers);

        Assert.Equal("allowed", allowed.Value);
    }
}
