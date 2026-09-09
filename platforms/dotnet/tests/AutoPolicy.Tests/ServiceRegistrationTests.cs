using Microsoft.Extensions.DependencyInjection;

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
        Assert.NotNull(scope.ServiceProvider.GetService<IUserPermissionProvider>());
    }

    [Fact]
    public void DefaultPolicyName_IsStable()
    {
        Assert.Equal("AutoPolicy", AutoPolicyDefaults.PolicyName);
    }
}
