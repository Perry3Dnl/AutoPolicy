using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

internal sealed class AutoPolicyAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _inner;
    private readonly IOptions<AutoPolicyOptions> _options;

    public AutoPolicyAuthorizationPolicyProvider(
        IAuthorizationPolicyProvider inner,
        IOptions<AutoPolicyOptions> options)
    {
        _inner = inner;
        _options = options;
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await _inner.GetPolicyAsync(policyName).ConfigureAwait(false);
        return AddAutoPolicyRequirement(policy, createIfMissing: false);
    }

    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        var policy = await _inner.GetDefaultPolicyAsync().ConfigureAwait(false);
        return AddAutoPolicyRequirement(policy, createIfMissing: true)!;
    }

    public async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        var policy = await _inner.GetFallbackPolicyAsync().ConfigureAwait(false);
        return AddAutoPolicyRequirement(policy, createIfMissing: true);
    }

    private AuthorizationPolicy? AddAutoPolicyRequirement(
        AuthorizationPolicy? policy,
        bool createIfMissing)
    {
        if (!_options.Value.MinimalApisProtectedByDefault)
        {
            return policy;
        }

        if (policy is null && !createIfMissing)
        {
            return null;
        }

        if (policy?.Requirements.OfType<AutoPolicyRequirement>().Any() == true)
        {
            return policy;
        }

        var builder = policy is null
            ? new AuthorizationPolicyBuilder()
            : new AuthorizationPolicyBuilder(policy);

        builder.AddRequirements(new AutoPolicyRequirement());
        return builder.Build();
    }
}
