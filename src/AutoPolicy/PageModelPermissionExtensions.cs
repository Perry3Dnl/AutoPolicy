using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

/// <summary>
/// Convenience access checks for Razor Page models.
/// </summary>
public static class PageModelPermissionExtensions
{
    public static AutoPolicyServices GetAutoPolicy(this PageModel page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.GetAutoPolicy();
    }

    /// <summary>
    /// Checks a discovered or explicitly registered AutoPolicy permission for the current request.
    /// </summary>
    public static ValueTask<bool> HasAccessAsync(
        this PageModel page,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.HasAccessAsync(permissionKey, cancellationToken);
    }

    /// <summary>
    /// Checks whether the current request has at least one of the supplied permissions.
    /// </summary>
    public static ValueTask<bool> HasAnyAccessAsync(
        this PageModel page,
        params string[] permissionKeys)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.HasAnyAccessAsync(permissionKeys);
    }

    /// <summary>
    /// Gets all currently effective permissions known to the AutoPolicy registry.
    /// </summary>
    public static ValueTask<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        this PageModel page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.GetEffectivePermissionsAsync(cancellationToken);
    }
}

/// <summary>
/// Convenience access checks for the current HTTP request.
/// </summary>
public static class HttpContextPermissionExtensions
{
    public static AutoPolicyServices GetAutoPolicy(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new AutoPolicyServices(httpContext);
    }

    /// <summary>
    /// Checks a discovered or explicitly registered AutoPolicy permission for the current request.
    /// </summary>
    public static ValueTask<bool> HasAccessAsync(
        this HttpContext httpContext,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.GetAutoPolicy().HasAccessAsync(permissionKey, cancellationToken);
    }

    /// <summary>
    /// Checks whether the current request has at least one of the supplied permissions.
    /// </summary>
    public static ValueTask<bool> HasAnyAccessAsync(
        this HttpContext httpContext,
        params string[] permissionKeys)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.GetAutoPolicy().HasAnyAccessAsync(permissionKeys);
    }

    /// <summary>
    /// Gets all currently effective permissions known to the AutoPolicy registry.
    /// </summary>
    public static ValueTask<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        this HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.GetAutoPolicy().GetEffectivePermissionsAsync(cancellationToken);
    }
}

/// <summary>
/// Provides request-scoped access to AutoPolicy services and access checks.
/// </summary>
public sealed class AutoPolicyServices
{
    private readonly HttpContext _httpContext;

    internal AutoPolicyServices(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    private IServiceProvider Services => _httpContext.RequestServices;

    private AutoPolicyOptions Options =>
        Services.GetRequiredService<IOptions<AutoPolicyOptions>>().Value;

    private ILogger<AutoPolicyServices>? Logger =>
        Services.GetService<ILogger<AutoPolicyServices>>();

    public IPermissionRegistry Registry =>
        Services.GetRequiredService<IPermissionRegistry>();

    public IPermissionEvaluator Evaluator =>
        Services.GetRequiredService<IPermissionEvaluator>();

    public IAutoPolicyAccessProvider Provider =>
        Services.GetRequiredService<IAutoPolicyAccessProvider>();

    /// <summary>
    /// Gets the host application's access snapshot, cached for the lifetime of this HTTP request.
    /// </summary>
    public ValueTask<AutoPolicyAccess> GetAccessAsync(
        CancellationToken cancellationToken = default) =>
        AutoPolicyAccessCache.GetAsync(_httpContext, Provider, cancellationToken);

    /// <summary>
    /// Checks a discovered or explicitly registered permission. Unknown keys fail closed.
    /// </summary>
    public async ValueTask<bool> HasAccessAsync(
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveKnownPermission(permissionKey, out var canonicalKey))
        {
            return false;
        }

        try
        {
            var access = await GetAccessAsync(cancellationToken).ConfigureAwait(false);
            return Evaluator.HasAccess(canonicalKey, access);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(
                ex,
                "AutoPolicy in-page access evaluation failed for {PermissionKey}. Denying access.",
                canonicalKey);
            return false;
        }
    }

    /// <summary>
    /// Checks whether at least one discovered or explicitly registered permission is allowed.
    /// Unknown keys are treated as denied.
    /// </summary>
    public async ValueTask<bool> HasAnyAccessAsync(
        IEnumerable<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionKeys);

        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var permissionKey in permissionKeys)
        {
            if (TryResolveKnownPermission(permissionKey, out var canonicalKey))
            {
                knownKeys.Add(canonicalKey);
            }
        }

        if (knownKeys.Count == 0)
        {
            return false;
        }

        try
        {
            var access = await GetAccessAsync(cancellationToken).ConfigureAwait(false);
            foreach (var permissionKey in knownKeys)
            {
                if (Evaluator.HasAccess(permissionKey, access))
                {
                    return true;
                }
            }

            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "AutoPolicy in-page access evaluation failed. Denying access.");
            return false;
        }
    }

    /// <summary>
    /// Gets all effective permissions from the current registry. Failures return an empty set.
    /// </summary>
    public async ValueTask<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await GetAccessAsync(cancellationToken).ConfigureAwait(false);
            return Evaluator.GetEffectivePermissions(access, Registry);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "AutoPolicy could not calculate effective permissions.");
            return Array.Empty<string>();
        }
    }

    public IReadOnlyCollection<string> GetEffectivePermissions(AutoPolicyAccess access) =>
        Evaluator.GetEffectivePermissions(access, Registry);

    private bool TryResolveKnownPermission(string permissionKey, out string canonicalKey)
    {
        try
        {
            canonicalKey = AutoPolicyPermissionKeyResolver.Resolve(permissionKey, Options);
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "AutoPolicy could not resolve permission key {PermissionKey}.", permissionKey);
            canonicalKey = string.Empty;
            return false;
        }

        if (Registry.Contains(canonicalKey))
        {
            return true;
        }

        Logger?.LogWarning(
            "AutoPolicy permission {PermissionKey} is not registered. Denying access. "
            + "Register non-route capabilities with DefinePermission(...).",
            canonicalKey);
        canonicalKey = string.Empty;
        return false;
    }
}
