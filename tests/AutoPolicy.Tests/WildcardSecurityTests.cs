using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class WildcardSecurityTests
{
    [Fact]
    public void MatchAll_GrantsAnyConcretePermission()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = [PermissionPattern.MatchAll]
        };

        Assert.True(evaluator.HasAccess("/Administration/Accounts", access));
        Assert.True(evaluator.HasAccess("/Staff/Reports/Daily", access));
    }

    [Fact]
    public void ExactDeny_OverridesMatchAllAllow()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = [PermissionPattern.MatchAll],
            DenyPermissions = ["/Administration/Accounts/Secret"]
        };

        Assert.True(evaluator.HasAccess("/Administration/Accounts/List", access));
        Assert.False(evaluator.HasAccess("/Administration/Accounts/Secret", access));
    }

    [Fact]
    public void PrefixDeny_OverridesMatchAllAllow()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = [PermissionPattern.MatchAll],
            DenyPermissions = ["/Administration/Accounts/*"]
        };

        Assert.False(evaluator.HasAccess("/Administration/Accounts/List", access));
        Assert.False(evaluator.HasAccess("/Administration/Accounts/Secret/Detail", access));
        Assert.True(evaluator.HasAccess("/Administration/Settings", access));
    }

    [Fact]
    public void MatchAllDeny_OverridesExactRoleAndGroupAllows()
    {
        var model = new PermissionModel();
        model.DefineGroup("Reports", group => group.Include("/Reports/*"));
        model.DefineRole("Staff", role => role.IncludeGroup("Reports"));

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            AllowGroups = ["Reports"],
            AllowPermissions = ["/Reports/Daily"],
            DenyPermissions = [PermissionPattern.MatchAll]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Reports/Monthly", access));
    }

    [Fact]
    public void PrefixWildcard_MatchesOnlyTrueDescendants()
    {
        const string pattern = "/Admin/*";

        Assert.True(PermissionPattern.Matches("/Admin/Users", pattern));
        Assert.True(PermissionPattern.Matches("/Admin/Users/Detail", pattern));
        Assert.True(PermissionPattern.Matches("/admin/users", pattern));

        Assert.False(PermissionPattern.Matches("/Admin", pattern));
        Assert.False(PermissionPattern.Matches("/Administrator/Users", pattern));
        Assert.False(PermissionPattern.Matches("/Admin2/Users", pattern));
    }

    [Fact]
    public void PrefixWildcard_UsesCanonicalPermissionIdentity()
    {
        Assert.True(PermissionPattern.Matches(
            "\\Administration\\Accounts\\List?tab=all#top",
            "/Administration/Accounts/*"));
    }

    [Theory]
    [InlineData("/Admin*")]
    [InlineData("*/Admin")]
    [InlineData("**")]
    [InlineData("/Admin/**")]
    [InlineData("/Admin/*/Edit")]
    [InlineData("/Admin/*/Edit/*")]
    [InlineData("/*")]
    public void MalformedWildcardPatterns_AreRejected(string pattern)
    {
        Assert.Throws<ArgumentException>(() => PermissionPattern.Validate(pattern));
        Assert.Throws<ArgumentException>(() => PermissionPattern.Matches("/Admin/Users", pattern));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("/Admin/*")]
    [InlineData("/Admin/User*")]
    [InlineData("/Admin/*/Edit")]
    public void ConcretePermissionKeys_CannotContainWildcardCharacters(string key)
    {
        Assert.Throws<ArgumentException>(() => PermissionKey.Normalize(key));
    }

    [Fact]
    public void RoleMatchAll_StillHonorsDirectDeny()
    {
        var model = new PermissionModel();
        model.DefineRole("Administrator", role => role.Include(PermissionPattern.MatchAll));

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Administrator"],
            DenyPermissions = ["/Admin/Dangerous"]
        };

        Assert.True(evaluator.HasAccess("/Admin/Dashboard", access));
        Assert.False(evaluator.HasAccess("/Admin/Dangerous", access));
    }

    [Fact]
    public void EffectivePermissions_MatchAllExpandsOnlyRegistryAndAppliesDenies()
    {
        var registry = new PermissionRegistry();
        registry.TryAdd(new PermissionRegistration("/Admin/Dashboard"), "test");
        registry.TryAdd(new PermissionRegistration("/Admin/Secret"), "test");
        registry.TryAdd(new PermissionRegistration("/Reports/Daily"), "test");

        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = [PermissionPattern.MatchAll],
            DenyPermissions = ["/Admin/*"]
        };

        var effective = evaluator.GetEffectivePermissions(access, registry);

        Assert.DoesNotContain("/Admin/Dashboard", effective);
        Assert.DoesNotContain("/Admin/Secret", effective);
        Assert.Contains("/Reports/Daily", effective);
        Assert.Equal(1, effective.Count);
    }

    [Fact]
    public void EffectivePermissions_DoesNotReportUnknownExactGrant()
    {
        var registry = new PermissionRegistry();
        registry.TryAdd(new PermissionRegistration("/Known"), "test");

        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Known", "/Unknown"]
        };

        var effective = evaluator.GetEffectivePermissions(access, registry);

        Assert.Contains("/Known", effective);
        Assert.DoesNotContain("/Unknown", effective);
        Assert.Single(effective);
    }

    [Fact]
    public async Task InPageMatchAll_OnlyAuthorizesRegisteredCapabilities()
    {
        const string known = "/Features/Permissions/Edit";
        const string unknown = "/Features/Permissions/DeleteEverything";

        var accessProvider = new FixedAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = [PermissionPattern.MatchAll]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options => options.DefinePermission(known));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.True(await context.HasAccessAsync(known));
        Assert.False(await context.HasAccessAsync(unknown));
        Assert.Equal(1, accessProvider.CallCount);
    }

    [Fact]
    public async Task MalformedProviderPattern_FailsClosedForInPageCheck()
    {
        const string known = "/Admin/Users/Edit";

        var accessProvider = new FixedAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Admin/*/Broken/*"]
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAutoPolicyAccessProvider>(accessProvider);
        services.AddAutoPolicy(options => options.DefinePermission(known));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        Assert.False(await context.HasAccessAsync(known));
    }

    private sealed class FixedAccessProvider(AutoPolicyAccess access) : IAutoPolicyAccessProvider
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
