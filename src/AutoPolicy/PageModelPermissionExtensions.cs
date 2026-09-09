using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

/// <summary>
/// Provides request-scoped permission checks for Razor Page models.
/// </summary>
public static class PageModelPermissionExtensions
{
    /// <summary>
    /// Checks a discovered or explicitly registered AutoPolicy permission for the current request.
    /// Unknown permission keys fail closed.
    /// </summary>
    /// <param name="page">The current Razor Page model.</param>
    /// <param name="permissionKey">The concrete permission key to check.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns><see langword="true"/> when the current access snapshot allows the permission.</returns>
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
    /// Unknown permission keys are treated as denied.
    /// </summary>
    /// <param name="page">The current Razor Page model.</param>
    /// <param name="permissionKeys">Concrete permission keys to check.</param>
    /// <returns><see langword="true"/> when at least one registered permission is allowed.</returns>
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
    /// <param name="page">The current Razor Page model.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns>The registered permissions that remain allowed after deny-wins evaluation.</returns>
    public static ValueTask<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        this PageModel page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.HttpContext.GetEffectivePermissionsAsync(cancellationToken);
    }
}

/// <summary>
/// Provides request-scoped permission checks for <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextPermissionExtensions
{
    /// <summary>
    /// Checks a discovered or explicitly registered AutoPolicy permission for the current request.
    /// Unknown permission keys fail closed.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="permissionKey">The concrete permission key to check.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns><see langword="true"/> when the current access snapshot allows the permission.</returns>
    public static ValueTask<bool> HasAccessAsync(
        this HttpContext httpContext,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new AutoPolicyServices(httpContext).HasAccessAsync(permissionKey, cancellationToken);
    }

    /// <summary>
    /// Checks whether the current request has at least one of the supplied permissions.
    /// Unknown permission keys are treated as denied.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="permissionKeys">Concrete permission keys to check.</param>
    /// <returns><see langword="true"/> when at least one registered permission is allowed.</returns>
    public static ValueTask<bool> HasAnyAccessAsync(
        this HttpContext httpContext,
        params string[] permissionKeys)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new AutoPolicyServices(httpContext).HasAnyAccessAsync(permissionKeys);
    }

    /// <summary>
    /// Gets all currently effective permissions known to the AutoPolicy registry.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns>The registered permissions that remain allowed after deny-wins evaluation.</returns>
    public static ValueTask<IReadOnlyCollection<string>> GetEffectivePermissionsAsync(
        this HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return new AutoPolicyServices(httpContext).GetEffectivePermissionsAsync(cancellationToken);
    }
}

internal sealed class AutoPolicyServices
{
    private readonly HttpContext _httpContext;

    public AutoPolicyServices(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    private IServiceProvider Services => _httpContext.RequestServices;

    private AutoPolicyOptions Options =>
        Services.GetRequiredService<IOptions<AutoPolicyOptions>>().Value;

    private ILogger<AutoPolicyServices>? Logger =>
        Services.GetService<ILogger<AutoPolicyServices>>();

    private IPermissionRegistry Registry =>
        Services.GetRequiredService<IPermissionRegistry>();

    private IPermissionEvaluator Evaluator =>
        Services.GetRequiredService<IPermissionEvaluator>();

    private IAutoPolicyAccessProvider Provider =>
        Services.GetRequiredService<IAutoPolicyAccessProvider>();

    public ValueTask<AutoPolicyAccess> GetAccessAsync(
        CancellationToken cancellationToken = default) =>
        AutoPolicyAccessCache.GetAsync(_httpContext, Provider, cancellationToken);

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
