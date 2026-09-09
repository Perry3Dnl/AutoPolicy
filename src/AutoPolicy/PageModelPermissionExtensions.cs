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
        return new AutoPolicyServices(httpContext);
    }
}

public sealed class AutoPolicyServices
{
    private readonly HttpContext _httpContext;

    internal AutoPolicyServices(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    private IServiceProvider Services => _httpContext.RequestServices;

    public IPermissionRegistry Registry =>
        Services.GetRequiredService<IPermissionRegistry>();

    public IPermissionEvaluator Evaluator =>
        Services.GetRequiredService<IPermissionEvaluator>();

    public IAutoPolicyAccessProvider Provider =>
        Services.GetRequiredService<IAutoPolicyAccessProvider>();

    public ValueTask<AutoPolicyAccess> GetAccessAsync(
        CancellationToken cancellationToken = default) =>
        Provider.GetAccessAsync(_httpContext, cancellationToken);

    public IReadOnlyCollection<string> GetEffectivePermissions(AutoPolicyAccess access) =>
        Evaluator.GetEffectivePermissions(access, Registry);
}
