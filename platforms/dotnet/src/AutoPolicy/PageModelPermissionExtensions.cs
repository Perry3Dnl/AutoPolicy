using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPolicy;

public static class PageModelPermissionExtensions
{
    public static AutoPolicyServices GetAutoPolicy(this PageModel page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.GetAutoPolicy();
    }
}

public static class HttpContextPermissionExtensions
{
    public static AutoPolicyServices GetAutoPolicy(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new AutoPolicyServices(httpContext.RequestServices);
    }
}

public sealed class AutoPolicyServices
{
    private readonly IServiceProvider _services;

    internal AutoPolicyServices(IServiceProvider services)
    {
        _services = services;
    }

    public IPermissionRegistry Registry =>
        _services.GetRequiredService<IPermissionRegistry>();

    public IPermissionEvaluator Evaluator =>
        _services.GetRequiredService<IPermissionEvaluator>();

    public IUserPermissionProvider Provider =>
        _services.GetRequiredService<IUserPermissionProvider>();

    public ValueTask<UserAccess> GetAccessAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default) =>
        Provider.GetAccessAsync(user, cancellationToken);

    public IReadOnlyCollection<string> GetEffectivePermissions(UserAccess access) =>
        Evaluator.GetEffectivePermissions(access, Registry);
}
