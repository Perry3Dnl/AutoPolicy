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
    private readonly IOptions<AutoPolicyOptions> _options;
    private readonly ILogger<AutoPolicyHandler> _logger;

    public AutoPolicyHandler(
        IAutoPolicyAccessProvider provider,
        IPermissionEvaluator evaluator,
        IOptions<AutoPolicyOptions> options,
        ILogger<AutoPolicyHandler> logger)
    {
        _provider = provider;
        _evaluator = evaluator;
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

        AutoPolicyAccess access;
        try
        {
            access = await AutoPolicyAccessCache
                .GetAsync(httpContext, _provider, httpContext.RequestAborted)
                .ConfigureAwait(false);
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
        if (!string.IsNullOrWhiteSpace(requirement.PermissionKey))
        {
            key = Canonicalize(requirement.PermissionKey);
            return true;
        }

        var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<AutoPolicyMetadata>();
        if (metadata is not null)
        {
            key = Canonicalize(metadata.Key);
            return true;
        }

        key = string.Empty;
        return false;
    }

    private string Canonicalize(string permissionKey)
    {
        var normalized = PermissionKey.Normalize(permissionKey);
        var aliases = _options.Value.Aliases;
        if (aliases.TryGetValue(normalized, out var canonical))
        {
            return canonical;
        }

        return normalized;
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
