using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class AccessProviderTests
{
    [Fact]
    public async Task ClaimsProvider_MapsAuthenticatedClaimsToAutoPolicyAccess()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Staff"),
                new Claim(ClaimsAutoPolicyAccessProvider.DefaultGroupClaimType, "Members"),
                new Claim(ClaimsAutoPolicyAccessProvider.DefaultAllowPermissionClaimType, "/Reports/*"),
                new Claim(ClaimsAutoPolicyAccessProvider.DefaultDenyPermissionClaimType, "/Reports/Secret")
            ], "test"))
        };

        var provider = new ClaimsAutoPolicyAccessProvider();
        var access = await provider.GetAccessAsync(context);

        Assert.Contains("Staff", access.AllowRoles);
        Assert.Contains("Members", access.AllowGroups);
        Assert.Contains("/Reports/*", access.AllowPermissions);
        Assert.Contains("/Reports/Secret", access.DenyPermissions);
    }

    [Fact]
    public async Task ClaimsProvider_AnonymousPrincipalProducesEmptyAccess()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Staff"),
                new Claim(ClaimsAutoPolicyAccessProvider.DefaultAllowPermissionClaimType, "/Admin/*")
            ]))
        };

        var provider = new ClaimsAutoPolicyAccessProvider();
        var access = await provider.GetAccessAsync(context);

        Assert.True(access.IsEmpty);
    }

    [Fact]
    public async Task Handler_CanAuthorizeFromCustomProviderWithoutAuthenticatedPrincipal()
    {
        var provider = new FixedAccessProvider(new AutoPolicyAccess
        {
            AllowPermissions = ["/Secure"]
        });
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var options = Options.Create(new AutoPolicyOptions());
        var handler = new AutoPolicyHandler(
            provider,
            evaluator,
            options,
            NullLogger<AutoPolicyHandler>.Instance);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AutoPolicyMetadata("/Secure")),
            "secure"));

        var requirement = new AutoPolicyRequirement();
        var authorizationContext = new AuthorizationHandlerContext(
            [requirement],
            httpContext.User,
            httpContext);

        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
    }

    private sealed class FixedAccessProvider(AutoPolicyAccess access) : IAutoPolicyAccessProvider
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
}
