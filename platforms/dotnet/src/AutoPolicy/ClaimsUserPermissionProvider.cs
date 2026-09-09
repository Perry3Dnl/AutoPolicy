using System.Security.Claims;

namespace AutoPolicy;

/// <summary>
/// Optional claims-based access source. Claim type names are application-defined.
/// </summary>
public sealed class ClaimsUserPermissionProvider : IUserPermissionProvider
{
    public const string DefaultGroupClaimType = "permission_group";
    public const string DefaultAllowPermissionClaimType = "permission_allow";
    public const string DefaultDenyPermissionClaimType = "permission_deny";
    public const string DefaultDenyRoleClaimType = "permission_deny_role";
    public const string DefaultDenyGroupClaimType = "permission_deny_group";

    private readonly ClaimsPermissionOptions _options;

    public ClaimsUserPermissionProvider(ClaimsPermissionOptions? options = null)
    {
        _options = options ?? new ClaimsPermissionOptions();
    }

    public ValueTask<UserAccess> GetAccessAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        _ = cancellationToken;

        var access = new UserAccess
        {
            AllowRoles = GetClaims(user, _options.RoleClaimType),
            AllowGroups = GetClaims(user, _options.GroupClaimType),
            AllowPermissions = GetClaims(user, _options.AllowPermissionClaimType),
            DenyRoles = GetClaims(user, _options.DenyRoleClaimType),
            DenyGroups = GetClaims(user, _options.DenyGroupClaimType),
            DenyPermissions = GetClaims(user, _options.DenyPermissionClaimType)
        };

        return ValueTask.FromResult(access);
    }

    private static IReadOnlyCollection<string> GetClaims(ClaimsPrincipal user, string claimType) =>
        user.FindAll(claimType)
            .Select(claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
}

public sealed class ClaimsPermissionOptions
{
    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    public string GroupClaimType { get; set; } = ClaimsUserPermissionProvider.DefaultGroupClaimType;

    public string AllowPermissionClaimType { get; set; } = ClaimsUserPermissionProvider.DefaultAllowPermissionClaimType;

    public string DenyPermissionClaimType { get; set; } = ClaimsUserPermissionProvider.DefaultDenyPermissionClaimType;

    public string DenyRoleClaimType { get; set; } = ClaimsUserPermissionProvider.DefaultDenyRoleClaimType;

    public string DenyGroupClaimType { get; set; } = ClaimsUserPermissionProvider.DefaultDenyGroupClaimType;
}
