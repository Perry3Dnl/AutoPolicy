using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

/// <summary>
/// Applies AutoPolicy-specific permission-denied response behavior while delegating every other
/// authorization result to the application's existing middleware result handler.
/// </summary>
internal sealed class AutoPolicyAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _fallback;
    private readonly IOptions<AutoPolicyOptions> _options;

    public AutoPolicyAuthorizationMiddlewareResultHandler(
        IOptions<AutoPolicyOptions> options,
        IAuthorizationMiddlewareResultHandler fallback)
    {
        _options = options;
        _fallback = fallback;
    }

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (_options.Value.PermissionDeniedBehavior == PermissionDeniedBehavior.StatusCode403
            && authorizeResult.Forbidden
            && policy.Requirements.OfType<AutoPolicyRequirement>().Any())
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
