using AutoPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class SignalRAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HubMethodsRequireTheirOwnPermission()
    {
        await using var deniedConnection = CreateConnection();
        await deniedConnection.StartAsync();

        await Assert.ThrowsAsync<HubException>(
            () => deniedConnection.InvokeAsync<string>("Echo", "denied"));

        await using var allowedConnection = CreateConnection("/SignalR/Probe/Echo");
        await allowedConnection.StartAsync();

        Assert.Equal("allowed", await allowedConnection.InvokeAsync<string>("Echo", "allowed"));
    }

    private HubConnection CreateConnection(params string[] methodPermissions)
    {
        var handler = _factory.Server.CreateHandler();
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/probe"), options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
                options.Transports = HttpTransportType.LongPolling;
                options.Headers.Add(
                    "X-AutoPolicy-Allow",
                    string.Join(",", new[] { "/hubs/*" }.Concat(methodPermissions)));
            })
            .Build();

        return connection;
    }
}
