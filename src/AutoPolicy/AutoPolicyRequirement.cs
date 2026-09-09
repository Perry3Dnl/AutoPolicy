using Microsoft.AspNetCore.Authorization;

namespace AutoPolicy;

internal sealed class AutoPolicyRequirement : IAuthorizationRequirement
{
    public AutoPolicyRequirement(string? permissionKey = null)
    {
        PermissionKey = permissionKey is null
            ? null
            : AutoPolicy.PermissionKey.Normalize(permissionKey);
    }

    public string? PermissionKey { get; }
}
