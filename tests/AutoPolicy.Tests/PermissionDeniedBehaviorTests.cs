using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class PermissionDeniedBehaviorTests
{
    [Fact]
    public async Task StatusCode403_Returns403ForAutoPolicyForbidWithoutCallingFallback()
    {
        var fallback = new TrackingResultHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(fallback);
        services.AddAutoPolicy(options =>
        {
            options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
        });

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        var context = new DefaultHttpContext { RequestServices = provider };
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new AutoPolicyRequirement())
            .Build();

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task Default_DelegatesAutoPolicyForbidToExistingApplicationHandler()
    {
        var fallback = new TrackingResultHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(fallback);
        services.AddAutoPolicy();

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        var context = new DefaultHttpContext { RequestServices = provider };
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new AutoPolicyRequirement())
            .Build();

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task StatusCode403_DoesNotChangeUnrelatedAuthorizationPolicies()
    {
        var fallback = new TrackingResultHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(fallback);
        services.AddAutoPolicy(options =>
        {
            options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
        });

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>();
        var context = new DefaultHttpContext { RequestServices = provider };
        var policy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => false)
            .Build();

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(1, fallback.CallCount);
    }

    private sealed class TrackingResultHandler : IAuthorizationMiddlewareResultHandler
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            _ = next;
            _ = context;
            _ = policy;
            _ = authorizeResult;
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
