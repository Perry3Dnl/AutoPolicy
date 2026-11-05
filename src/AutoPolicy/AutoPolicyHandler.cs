using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using BlazorRouteData = Microsoft.AspNetCore.Components.RouteData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

internal sealed class AutoPolicyHandler : AuthorizationHandler<AutoPolicyRequirement>
{
    private readonly IAutoPolicyAccessProvider _provider;
    private readonly IPermissionEvaluator _evaluator;
    private readonly IPermissionRegistry _registry;
    private readonly IOptions<AutoPolicyOptions> _options;
    private readonly ILogger<AutoPolicyHandler> _logger;

    public AutoPolicyHandler(
        IAutoPolicyAccessProvider provider,
        IPermissionEvaluator evaluator,
        IPermissionRegistry registry,
        IOptions<AutoPolicyOptions> options,
        ILogger<AutoPolicyHandler> logger)
    {
        _provider = provider;
        _evaluator = evaluator;
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AutoPolicyRequirement requirement)
    {
        var httpContext = ResolveHttpContext(context);
        var endpointMetadata = httpContext?.GetEndpoint()?.Metadata.GetMetadata<AutoPolicyMetadata>();
        if (endpointMetadata is { IsProtected: false })
        {
            context.Succeed(requirement);
            return;
        }

        if (!TryResolvePermissionKey(context, httpContext, requirement, out var key))
        {
            _logger.LogWarning("Permission mapping could not be resolved for the current authorization resource. Denying access.");
            context.Fail();
            return;
        }

        if (!_registry.Contains(key)
            && string.IsNullOrWhiteSpace(requirement.PermissionKey)
            && _options.Value.EndpointsProtectedByDefault
            && _registry is PermissionRegistry mutableRegistry)
        {
            mutableRegistry.TryAdd(
                new PermissionRegistration(
                    key,
                    relativePath: httpContext?.GetEndpoint()?.DisplayName
                        ?? GetAuthorizationResourceName(context.Resource),
                    isProtected: true),
                httpContext?.GetEndpoint()?.DisplayName
                    ?? GetAuthorizationResourceName(context.Resource)
                    ?? "ASP.NET Core endpoint");
        }

        if (!_registry.Contains(key))
        {
            _logger.LogWarning(
                "AutoPolicy permission {PermissionKey} is not registered. Denying the request.",
                key);
            context.Fail();
            return;
        }

        AutoPolicyAccess access;
        try
        {
            if (httpContext is not null)
            {
                access = await AutoPolicyAccessCache
                    .GetAsync(httpContext, _provider, httpContext.RequestAborted)
                    .ConfigureAwait(false);
            }
            else if (_provider is IAutoPolicyPrincipalAccessProvider principalProvider)
            {
                access = await principalProvider
                    .GetAccessAsync(context.User)
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "The configured IAutoPolicyAccessProvider does not support principal-only authorization. Denying access.");
                context.Fail();
                return;
            }
        }
        catch (OperationCanceledException) when (
            httpContext is not null && httpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The AutoPolicy access provider failed. Denying the request.");
            context.Fail();
            return;
        }

        try
        {
            if (_evaluator.HasAccess(key, access))
            {
                context.Succeed(requirement);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permission evaluation failed for {PermissionKey}. Denying the request.", key);
            context.Fail();
            return;
        }

        context.Fail();
    }

    private bool TryResolvePermissionKey(
        AuthorizationHandlerContext context,
        HttpContext? httpContext,
        AutoPolicyRequirement requirement,
        out string key)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(requirement.PermissionKey))
            {
                key = AutoPolicyPermissionKeyResolver.Resolve(requirement.PermissionKey, _options.Value);
                return true;
            }

            if (context.Resource is HubInvocationContext hubInvocation
                && TryResolveHubPermissionKey(hubInvocation, out var hubKey))
            {
                key = hubKey;
                return true;
            }

            if (context.Resource is BlazorRouteData routeData
                && TryResolveComponentPermissionKey(routeData, out var componentKey))
            {
                key = componentKey;
                return true;
            }

            if (httpContext is not null
                && TryResolveHttpPermissionKey(httpContext, out var httpKey))
            {
                key = httpKey;
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoPolicy failed to canonicalize the required permission key.");
        }

        key = string.Empty;
        return false;
    }

    private bool TryResolveHttpPermissionKey(HttpContext httpContext, out string key)
    {
        var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<AutoPolicyMetadata>();
        var endpointKey = metadata?.Key;
        if (string.IsNullOrWhiteSpace(endpointKey)
            && httpContext.GetEndpoint() is RouteEndpoint routeEndpoint)
        {
            endpointKey = routeEndpoint.RoutePattern.RawText;
        }

        if (!string.IsNullOrWhiteSpace(endpointKey))
        {
            key = AutoPolicyPermissionKeyResolver.Resolve(endpointKey, _options.Value);
            return true;
        }

        key = string.Empty;
        return false;
    }

    private bool TryResolveComponentPermissionKey(BlazorRouteData routeData, out string key)
    {
        var template = routeData.Template;
        if (string.IsNullOrWhiteSpace(template))
        {
            var routeAttributes = routeData.PageType
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(attribute => attribute.Template)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (routeAttributes.Length == 1)
            {
                template = routeAttributes[0];
            }
        }

        if (!string.IsNullOrWhiteSpace(template))
        {
            key = AutoPolicyPermissionKeyResolver.Resolve(template, _options.Value);
            return true;
        }

        _logger.LogWarning(
            "Blazor route component {ComponentType} has no unique route template. Denying access.",
            routeData.PageType.FullName);
        key = string.Empty;
        return false;
    }

    private bool TryResolveHubPermissionKey(HubInvocationContext hubInvocation, out string key)
    {
        var hubName = hubInvocation.Hub.GetType().Name;
        if (hubName.EndsWith("Hub", StringComparison.Ordinal))
        {
            hubName = hubName[..^3];
        }

        key = AutoPolicyPermissionKeyResolver.Resolve(
            $"/SignalR/{hubName}/{hubInvocation.HubMethodName}",
            _options.Value);
        return true;
    }

    private static HttpContext? ResolveHttpContext(AuthorizationHandlerContext context)
    {
        switch (context.Resource)
        {
            case HttpContext httpContext:
                return httpContext;
            case AuthorizationFilterContext filterContext:
                return filterContext.HttpContext;
            case HubInvocationContext hubInvocation:
                return hubInvocation.Context.GetHttpContext();
            default:
                return null;
        }
    }

    private static string? GetAuthorizationResourceName(object? resource) =>
        resource switch
        {
            BlazorRouteData routeData => routeData.PageType.FullName,
            HubInvocationContext hubInvocation => hubInvocation.Hub.GetType().FullName,
            _ => null
        };
}
