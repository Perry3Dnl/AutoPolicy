namespace AutoPolicy;

public sealed class AutoPolicyOptions
{
    private readonly HashSet<string> _explicitPermissions =
        new(StringComparer.OrdinalIgnoreCase);

    public PermissionModel Model { get; } = new();

    public bool RazorPagesProtectedByDefault { get; set; } = true;

    public bool StrictValidation { get; set; }

    public IList<string> AnonymousPatterns { get; } = new List<string>();

    /// <summary>
    /// Gets explicitly registered non-route permissions, such as permissions used by partials,
    /// page sections, buttons, menu items, or other application capabilities.
    /// </summary>
    public IReadOnlyCollection<string> ExplicitPermissions => _explicitPermissions;

    public IDictionary<string, string> PermissionKeyOverrides { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Aliases { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public AutoPolicyOptions ProtectRazorPagesByDefault(bool enabled = true)
    {
        RazorPagesProtectedByDefault = enabled;
        return this;
    }

    public AutoPolicyOptions AllowAnonymous(params string[] patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        foreach (var pattern in patterns)
        {
            PermissionPattern.Validate(pattern);
            AnonymousPatterns.Add(pattern.Trim());
        }

        return this;
    }

    /// <summary>
    /// Registers one or more permissions that are not discovered from Razor Page routes.
    /// Explicit permissions participate in roles, groups, wildcard matching, deny-wins evaluation,
    /// registry diagnostics, and in-page access checks exactly like discovered page permissions.
    /// </summary>
    public AutoPolicyOptions DefinePermission(params string[] permissionKeys)
    {
        ArgumentNullException.ThrowIfNull(permissionKeys);

        foreach (var permissionKey in permissionKeys)
        {
            _explicitPermissions.Add(PermissionKey.Normalize(permissionKey));
        }

        return this;
    }

    public AutoPolicyOptions DefineGroup(string name, Action<PermissionGroupBuilder> configure)
    {
        Model.DefineGroup(name, configure);
        return this;
    }

    public AutoPolicyOptions DefineRole(string name, Action<PermissionRoleBuilder> configure)
    {
        Model.DefineRole(name, configure);
        return this;
    }

    public AutoPolicyOptions OverridePermissionKey(string pagePath, string permissionKey)
    {
        PermissionKeyOverrides[PermissionKey.Normalize(pagePath)] = PermissionKey.Normalize(permissionKey);
        return this;
    }

    public AutoPolicyOptions AddAlias(string alias, string canonicalKey)
    {
        Aliases[PermissionKey.Normalize(alias)] = PermissionKey.Normalize(canonicalKey);
        return this;
    }
}
