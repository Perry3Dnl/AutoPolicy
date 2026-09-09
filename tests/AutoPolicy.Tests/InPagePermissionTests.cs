using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class InPagePermissionTests
{
    private const string EditRoles = "/Administration/Accounts/AccountPermissions/EditRoles";
    private const string ViewHistory = "/Administration/Accounts/AccountPermissions/ViewHistory";

    [Fact]
    public async Task HasAccessAsync_UsesExplicitPermissionsAndCachesProviderPerRequest()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowGroups = ["PermissionEditors"],
            AllowPermissions = [ViewHistory]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options =>
        {
            options.DefinePermission(EditRoles, ViewHistory);
            options.DefineGroup("PermissionEditors", group => group.Include(EditRoles));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.True(await context.HasAccessAsync(EditRoles));
        Assert.True(await context.HasAccessAsync(ViewHistory));
        Assert.True(await context.HasAnyAccessAsync("/Unknown/Capability", ViewHistory));
        Assert.Equal(1, accessProvider.CallCount);
    }

    [Fact]
    public async Task HasAccessAsync_UnknownPermissionFailsClosedEvenWhenWildcardWouldMatch()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Administration/*"]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.False(await context.HasAccessAsync(EditRoles));
        Assert.Equal(0, accessProvider.CallCount);
    }

    [Fact]
    public async Task HasAccessAsync_DirectDenyWinsForExplicitCapability()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Administration/Accounts/*"],
            DenyPermissions = [EditRoles]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options => options.DefinePermission(EditRoles));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.False(await context.HasAccessAsync(EditRoles));
    }

    [Fact]
    public async Task HasAccessAsync_ResolvesAliasToRegisteredCapability()
    {
        var accessProvider = new CountingAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = [EditRoles]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options =>
        {
            options.DefinePermission(EditRoles);
            options.AddAlias("/Legacy/EditRoles", EditRoles);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.True(await context.HasAccessAsync("/Legacy/EditRoles"));
    }

    [Fact]
    public void DefinePermission_RegistersConcreteCapabilityAndRejectsWildcardIdentity()
    {
        var options = new AutoPolicyOptions();
        options.DefinePermission(EditRoles);

        Assert.Contains(EditRoles, options.ExplicitPermissions);
        Assert.Throws<ArgumentException>(() => options.DefinePermission("/Administration/*"));
    }

    private sealed class CountingAccessProvider(AutoPolicyAccess access) : IAutoPolicyAccessProvider
    {
        private int _callCount;

        public int CallCount => _callCount;

        public ValueTask<AutoPolicyAccess> GetAccessAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            Interlocked.Increment(ref _callCount);
            return ValueTask.FromResult(access);
        }
    }
}
