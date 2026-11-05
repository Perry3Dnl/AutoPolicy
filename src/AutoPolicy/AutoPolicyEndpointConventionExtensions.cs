using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AutoPolicy;

/// <summary>
/// Adds AutoPolicy authorization metadata to Minimal API endpoints and route groups.
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
        });

        return builder;
    }
}
