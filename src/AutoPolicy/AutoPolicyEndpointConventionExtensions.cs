using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

/// <summary>
/// Adds AutoPolicy authorization metadata to ASP.NET Core endpoint convention builders.
/// </summary>
public static class AutoPolicyEndpointConventionExtensions
{
    /// <summary>
    /// Requires an AutoPolicy permission for the endpoint or route group.
    /// </summary>
    /// <remarks>
    /// When <paramref name="permissionKey"/> is omitted, AutoPolicy uses the endpoint's route pattern
    /// as its permission key. Each protected endpoint is discovered and registered at application startup.
    /// Access is denied when the current request has no matching grant.
    /// </remarks>
    /// <typeparam name="TBuilder">An endpoint or route-group convention builder.</typeparam>
    /// <param name="builder">The endpoint or route group to protect.</param>
    /// <param name="permissionKey">
    /// An optional explicit permission key. If omitted, the route pattern supplies the key.
    /// </param>
    /// <returns>The same builder for fluent endpoint configuration.</returns>
    public static TBuilder RequireAutoPolicy<TBuilder>(
        this TBuilder builder,
        string? permissionKey = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var normalizedKey = permissionKey is null
            ? null
            : AutoPolicy.PermissionKey.Normalize(permissionKey);

        builder.Add(endpointBuilder =>
        {
            var endpointPermissionKey = normalizedKey;
            if (endpointPermissionKey is null
                && endpointBuilder is RouteEndpointBuilder routeEndpointBuilder)
            {
                endpointPermissionKey = routeEndpointBuilder.RoutePattern.RawText;
            }

            endpointBuilder.Metadata.Add(new AutoPolicyMetadata(endpointPermissionKey));
            endpointBuilder.Metadata.Add(new AuthorizeAttribute(AutoPolicyDefaults.PolicyName));
            RegisterPermission(endpointBuilder, endpointPermissionKey);
        });

        return builder;
    }

    private static void RegisterPermission(EndpointBuilder endpointBuilder, string? permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return;
        }

        var services = endpointBuilder.ApplicationServices;
        var registry = services.GetService<PermissionRegistry>();
        var options = services.GetService<IOptions<AutoPolicyOptions>>()?.Value;
        if (registry is null)
        {
            return;
        }

        var canonicalKey = options is null
            ? AutoPolicy.PermissionKey.Normalize(permissionKey)
            : AutoPolicyPermissionKeyResolver.Resolve(permissionKey, options);

        if (!registry.Contains(canonicalKey))
        {
            registry.TryAdd(
                new PermissionRegistration(
                    canonicalKey,
                    relativePath: endpointBuilder.DisplayName,
                    isProtected: true),
                endpointBuilder.DisplayName ?? "Minimal API endpoint");
        }
    }
}
