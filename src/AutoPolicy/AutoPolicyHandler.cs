using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

public sealed class AutoPolicyHandler : AuthorizationHandler<AutoPolicyRequirement>
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
        if (httpContext is null)
        {
            _logger.LogWarning("AutoPolicy could not resolve the current HttpContext. Denying the request.");
            context.Fail();
            return;
        }

        if (!TryResolvePermissionKey(httpContext, requirement, out var key))
        {
            _logger.LogWarning("Path permission mapping could not be resolved. Denying the request.");
            context.Fail();
            return;
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
            access = await AutoPolicyAccessCache
                .GetAsync(httpContext, _provider, httpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IAutoPolicyAccessProvider failed while loading access. Denying the request.");
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
        HttpContext httpContext,
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

            var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<AutoPolicyMetadata>();
            if (metadata is not null)
            {
                key = AutoPolicyPermissionKeyResolver.Resolve(metadata.Key, _options.Value);
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

    private static HttpContext? ResolveHttpContext(AuthorizationHandlerContext context)
    {
        switch (context.Resource)
        {
            case HttpContext httpContext:
                return httpContext;
            case AuthorizationFilterContext filterContext:
                return filterContext.HttpContext;
            default:
                return null;
        }
    }
}
