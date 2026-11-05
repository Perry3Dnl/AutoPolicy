using System.Security.Claims;
using AutoPolicy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class BlazorAuthorizationTests
{
    [Fact]
    public async Task RoutedComponent_UsesRouteTemplateAndPrincipalAccessProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddAutoPolicy(options => options.DefinePermission("/blazor/probe"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var routeData = new RouteData(
            typeof(BlazorProbeComponent),
            new Dictionary<string, object?>());

        var grantedUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimsAutoPolicyAccessProvider.DefaultAllowPermissionClaimType, "/blazor/probe")],
            authenticationType: "test"));
        var deniedUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "test")],
            authenticationType: "test"));

        var granted = await authorization.AuthorizeAsync(grantedUser, routeData, "AutoPolicy");
        var denied = await authorization.AuthorizeAsync(deniedUser, routeData, "AutoPolicy");

        Assert.True(granted.Succeeded);
        Assert.False(denied.Succeeded);
    }
}

[Route("/blazor/probe")]
public sealed class BlazorProbeComponent : ComponentBase;
