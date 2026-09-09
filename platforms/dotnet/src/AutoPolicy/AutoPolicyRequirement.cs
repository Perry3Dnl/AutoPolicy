using Microsoft.AspNetCore.Authorization;

namespace AutoPolicy;

public sealed class AutoPolicyRequirement : IAuthorizationRequirement
{
    public AutoPolicyRequirement(string? permissionKey = null)
    {
        PermissionKey = permissionKey;
    }

    public string? PermissionKey { get; }
}
