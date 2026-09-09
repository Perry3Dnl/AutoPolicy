using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AutoPolicy;

/// <summary>
/// Built-in access provider that maps authenticated <see cref="ClaimsPrincipal"/> claims to AutoPolicy access.
/// </summary>
public sealed class ClaimsAutoPolicyAccessProvider : IAutoPolicyAccessProvider
{
    /// <summary>
    /// Default claim type used for allowed groups.
    /// </summary>
    public const string DefaultGroupClaimType = "permission_group";

    /// <summary>
    /// Default claim type used for direct allowed permissions.
    /// </summary>
    public const string DefaultAllowPermissionClaimType = "permission_allow";

    /// <summary>
    /// Default claim type used for direct denied permissions.
    /// </summary>
    public const string DefaultDenyPermissionClaimType = "permission_deny";

    /// <summary>
    /// Default claim type used for denied roles.
    /// </summary>
    public const string DefaultDenyRoleClaimType = "permission_deny_role";

    /// <summary>
    /// Default claim type used for denied groups.
    /// </summary>
    public const string DefaultDenyGroupClaimType = "permission_deny_group";

    private readonly ClaimsPermissionOptions _options;

    /// <summary>
    /// Creates a provider using the default claim-type mappings.
    /// </summary>
    public ClaimsAutoPolicyAccessProvider()
        : this(new ClaimsPermissionOptions())
    {
    }

    /// <summary>
    /// Creates a provider using custom claim-type mappings.
    /// </summary>
    /// <param name="options">Claim-type mappings used to build AutoPolicy access.</param>
    public ClaimsAutoPolicyAccessProvider(ClaimsPermissionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = cancellationToken;

        var user = context.User;
        if (!user.Identities.Any(static identity => identity.IsAuthenticated))
        {
            return ValueTask.FromResult(AutoPolicyAccess.Empty);
        }

        var access = new AutoPolicyAccess
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
        user.Identities
            .Where(static identity => identity.IsAuthenticated)
            .SelectMany(identity => identity.FindAll(claimType))
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
}

/// <summary>
/// Configures claim types understood by <see cref="ClaimsAutoPolicyAccessProvider"/>.
/// </summary>
public sealed class ClaimsPermissionOptions
{
    /// <summary>
    /// Gets or sets the claim type used for allowed roles.
    /// </summary>
    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// Gets or sets the claim type used for allowed groups.
    /// </summary>
    public string GroupClaimType { get; set; } = ClaimsAutoPolicyAccessProvider.DefaultGroupClaimType;

    /// <summary>
    /// Gets or sets the claim type used for direct allowed permissions.
    /// </summary>
    public string AllowPermissionClaimType { get; set; } = ClaimsAutoPolicyAccessProvider.DefaultAllowPermissionClaimType;

    /// <summary>
    /// Gets or sets the claim type used for direct denied permissions.
    /// </summary>
    public string DenyPermissionClaimType { get; set; } = ClaimsAutoPolicyAccessProvider.DefaultDenyPermissionClaimType;

    /// <summary>
    /// Gets or sets the claim type used for denied roles.
    /// </summary>
    public string DenyRoleClaimType { get; set; } = ClaimsAutoPolicyAccessProvider.DefaultDenyRoleClaimType;

    /// <summary>
    /// Gets or sets the claim type used for denied groups.
    /// </summary>
    public string DenyGroupClaimType { get; set; } = ClaimsAutoPolicyAccessProvider.DefaultDenyGroupClaimType;
}
