using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class FailureBehaviorTests
{
    [Fact]
    public async Task ProviderException_DeniesAuthorization()
    {
        var handler = CreateHandler(new ThrowingProvider(new InvalidOperationException("boom")));
        var context = CreateAuthorizationContext(out _);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task NullAccessSnapshot_DeniesAuthorization()
    {
        var handler = CreateHandler(new NullProvider());
        var context = CreateAuthorizationContext(out _);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task MalformedProviderPermission_DeniesAuthorization()
    {
        var handler = CreateHandler(new FixedProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Secure/**"]
        }));
        var context = CreateAuthorizationContext(out _);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task RequestCancellation_PropagatesInsteadOfBecomingPermissionDenied()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var handler = CreateHandler(new CancelledProvider());
        var context = CreateAuthorizationContext(out var httpContext);
        httpContext.RequestAborted = cancellation.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(context));

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task InPageProviderException_FailsClosed()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(
            new ThrowingProvider(new InvalidOperationException("boom")));
        services.AddAutoPolicy(options => options.DefinePermission("/Secure"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.False(await httpContext.HasAccessAsync("/Secure"));
        Assert.Empty(await httpContext.GetEffectivePermissionsAsync());
    }

    [Fact]
    public async Task InPageCancellation_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(new CancelledProvider());
        services.AddAutoPolicy(options => options.DefinePermission("/Secure"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await httpContext.HasAccessAsync("/Secure", cancellation.Token));
    }

    private static AutoPolicyHandler CreateHandler(IAutoPolicyAccessProvider provider)
    {
        var registry = new PermissionRegistry();
        registry.TryAdd(new PermissionRegistration("/Secure"), "test");

        return new AutoPolicyHandler(
            provider,
            new PermissionEvaluator(new PermissionModel()),
            registry,
            Options.Create(new AutoPolicyOptions()),
            NullLogger<AutoPolicyHandler>.Instance);
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(out DefaultHttpContext httpContext)
    {
        httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AutoPolicyMetadata("/Secure")),
            "secure"));

        var requirement = new AutoPolicyRequirement();
        return new AuthorizationHandlerContext(
            [requirement],
            httpContext.User,
            httpContext);
    }

    private sealed class FixedProvider(AutoPolicyAccess access) : IAutoPolicyAccessProvider
    {
        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            return ValueTask.FromResult(access);
        }
    }

    private sealed class ThrowingProvider(Exception exception) : IAutoPolicyAccessProvider
    {
        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            return ValueTask.FromException<AutoPolicyAccess>(exception);
        }
    }

    private sealed class NullProvider : IAutoPolicyAccessProvider
    {
        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            return ValueTask.FromResult<AutoPolicyAccess>(null!);
        }
    }

    private sealed class CancelledProvider : IAutoPolicyAccessProvider
    {
        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            return ValueTask.FromCanceled<AutoPolicyAccess>(cancellationToken);
        }
    }
}
