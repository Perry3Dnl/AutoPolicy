using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddAutoPolicy_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoPolicy();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IPermissionRegistry>());
        Assert.NotNull(scope.ServiceProvider.GetService<IPermissionEvaluator>());
        Assert.IsType<ClaimsAutoPolicyAccessProvider>(
            scope.ServiceProvider.GetRequiredService<IAutoPolicyAccessProvider>());
    }

    [Fact]
    public void AddAutoPolicy_PreservesCustomAccessProviderRegisteredBeforehand()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IAutoPolicyAccessProvider, TestAccessProvider>();
        services.AddAutoPolicy();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<TestAccessProvider>(
            scope.ServiceProvider.GetRequiredService<IAutoPolicyAccessProvider>());
    }

    [Fact]
    public void DefaultPolicyName_IsStable()
    {
        Assert.Equal("AutoPolicy", AutoPolicyDefaults.PolicyName);
    }

    private sealed class TestAccessProvider : IAutoPolicyAccessProvider
    {
        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            return ValueTask.FromResult(AutoPolicyAccess.Empty);
        }
    }
}
