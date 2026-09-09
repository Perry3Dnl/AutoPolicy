using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class InPageAccessTests
{
    [Fact]
    public async Task HasAccessAsync_UsesExplicitPermissionAndCachesProviderPerRequest()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Features/AccountPermissions/Edit"]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options =>
            options.DefinePermission("/Features/AccountPermissions/Edit"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        Assert.True(await context.HasAccessAsync("/Features/AccountPermissions/Edit"));
        Assert.True(await context.HasAccessAsync("/Features/AccountPermissions/Edit"));
        Assert.Equal(1, accessProvider.CallCount);
        Assert.Contains(
            "/Features/AccountPermissions/Edit",
            scope.ServiceProvider.GetRequiredService<IPermissionRegistry>().Keys);
    }

    [Fact]
    public async Task HasAccessAsync_DenyStillWinsForExplicitPermission()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Features/*"],
            DenyPermissions = ["/Features/AccountPermissions/Edit"]
        });

        var context = BuildContext(
            accessProvider,
            options => options.DefinePermission("/Features/AccountPermissions/Edit"),
            out var serviceProvider,
            out var scope);

        using (serviceProvider)
        using (scope)
        {
            Assert.False(await context.HasAccessAsync("/Features/AccountPermissions/Edit"));
        }
    }

    [Fact]
    public async Task HasAnyAccessAsync_UsesOneSnapshotForMultipleChecks()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Features/Profile/View"]
        });

        var context = BuildContext(
            accessProvider,
            options => options.DefinePermission(
                "/Features/Profile/View",
                "/Features/Profile/Edit"),
            out var serviceProvider,
            out var scope);

        using (serviceProvider)
        using (scope)
        {
            Assert.True(await context.HasAnyAccessAsync(
                "/Features/Profile/Edit",
                "/Features/Profile/View"));
            Assert.Equal(1, accessProvider.CallCount);
        }
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_IncludesExplicitPermissionsMatchedByWildcard()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Features/Profile/*"]
        });

        var context = BuildContext(
            accessProvider,
            options => options.DefinePermission(
                "/Features/Profile/View",
                "/Features/Profile/Edit"),
            out var serviceProvider,
            out var scope);

        using (serviceProvider)
        using (scope)
        {
            var permissions = await context.GetEffectivePermissionsAsync();

            Assert.Contains("/Features/Profile/View", permissions);
            Assert.Contains("/Features/Profile/Edit", permissions);
            Assert.Equal(1, accessProvider.CallCount);
        }
    }

    private static DefaultHttpContext BuildContext(
        IAutoPolicyAccessProvider accessProvider,
        Action<AutoPolicyOptions> configure,
        out ServiceProvider serviceProvider,
        out IServiceScope scope)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(accessProvider);
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(configure);

        serviceProvider = services.BuildServiceProvider();
        scope = serviceProvider.CreateScope();

        return new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
    }

    private sealed class CountingAccessProvider(AutoPolicyAccess access) : IAutoPolicyAccessProvider
    {
        public int CallCount { get; private set; }

        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            CallCount++;
            return ValueTask.FromResult(access);
        }
    }
}
