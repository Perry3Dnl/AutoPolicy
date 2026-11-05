using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutoPolicy;

internal sealed class AutoPolicyHubFilter : IHubFilter
{
    private readonly IAuthorizationService _authorizationService;

    public AutoPolicyHubFilter(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var result = await _authorizationService.AuthorizeAsync(
            invocationContext.Context.User ?? new System.Security.Claims.ClaimsPrincipal(),
            invocationContext,
            AutoPolicyDefaults.PolicyName).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new HubException("Access denied.");
        }

        return await next(invocationContext).ConfigureAwait(false);
    }
}
