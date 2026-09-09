using Microsoft.AspNetCore.Authorization;

namespace AutoPolicy;

public sealed class AutoPolicyRequirement : IAuthorizationRequirement
{
    public AutoPolicyRequirement(string? permissionKey = null)
    {
        PermissionKey = permissionKey is null
            ? null
            : AutoPolicy.PermissionKey.Normalize(permissionKey);
    }

    public string? PermissionKey { get; }
}
