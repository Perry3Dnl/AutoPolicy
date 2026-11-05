using Microsoft.AspNetCore.SignalR;

namespace AutoPolicy.TestApp.Hubs;

public sealed class ProbeHub : Hub
{
    public Task<string> Echo(string value) => Task.FromResult(value);
}
