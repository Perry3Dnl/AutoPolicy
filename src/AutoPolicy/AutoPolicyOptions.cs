using System.Collections.ObjectModel;

namespace AutoPolicy;

/// <summary>
/// Configures AutoPolicy permission discovery, permission definitions, and evaluation rules.
/// </summary>
/// <remarks>
/// AutoPolicy defines and evaluates permissions. The host application remains responsible for
/// identities, user accounts, persistence, and assigning roles, groups, or direct permissions.
/// </remarks>
public sealed class AutoPolicyOptions
{
    private readonly HashSet<string> _explicitPermissions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<string> _anonymousPatterns = [];
    private readonly Dictionary<string, string> _permissionKeyOverrides =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyList<string> _anonymousPatternsView;
    private readonly IReadOnlyDictionary<string, string> _permissionKeyOverridesView;
    private readonly IReadOnlyDictionary<string, string> _aliasesView;

    public AutoPolicyOptions()
    {
        _anonymousPatternsView = _anonymousPatterns.AsReadOnly();
        _permissionKeyOverridesView = new ReadOnlyDictionary<string, string>(_permissionKeyOverrides);
        _aliasesView = new ReadOnlyDictionary<string, string>(_aliases);
    }

    public PermissionModel Model { get; } = new();

    public bool RazorPagesProtectedByDefault { get; set; } = true;

    public bool StrictValidation { get; set; }

    /// <summary>
    /// Gets or sets how an AutoPolicy forbid is surfaced. The default delegates to the host
    /// application's normal ASP.NET Core authorization behavior.
    /// </summary>
    public PermissionDeniedBehavior PermissionDeniedBehavior { get; set; } = PermissionDeniedBehavior.Default;

    /// <summary>
    /// Gets the validated anonymous permission patterns configured through <see cref="AllowAnonymous(string[])"/>.
    /// </summary>
    public IReadOnlyList<string> AnonymousPatterns => _anonymousPatternsView;

    /// <summary>
    /// Gets explicitly registered non-route permissions, such as permissions used by partials,
    /// page sections, buttons, menu items, or other application capabilities.
    /// </summary>
    public IReadOnlyCollection<string> ExplicitPermissions => _explicitPermissions;

    /// <summary>
    /// Gets configured physical-page permission-key overrides.
    /// </summary>
    public IReadOnlyDictionary<string, string> PermissionKeyOverrides => _permissionKeyOverridesView;

    /// <summary>
    /// Gets configured legacy or alternate permission aliases.
    /// </summary>
    public IReadOnlyDictionary<string, string> Aliases => _aliasesView;

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
            var trimmed = pattern.Trim();

            if (trimmed == PermissionPattern.MatchAll)
            {
                throw new ArgumentException(
                    "The match-all '*' pattern cannot be used with AllowAnonymous. "
                    + "Use ProtectRazorPagesByDefault(false) for an intentional global opt-out.",
                    nameof(patterns));
            }

            _anonymousPatterns.Add(trimmed);
        }

        return this;
    }

    /// <summary>
    /// Registers one or more concrete permissions that are not discovered from Razor Page routes.
    /// </summary>
    /// <remarks>
    /// Use this for partials, page sections, buttons, menu items, and application capabilities that
    /// require a permission identity of their own. Explicit permissions participate in roles,
    /// groups, wildcard matching, deny-wins evaluation, registry diagnostics, and in-page checks
    /// exactly like discovered page permissions.
    /// </remarks>
    public AutoPolicyOptions DefinePermission(params string[] permissionKeys)
    {
        ArgumentNullException.ThrowIfNull(permissionKeys);

        foreach (var permissionKey in permissionKeys)
        {
            PermissionPattern.Validate(permissionKey);
            if (PermissionPattern.IsWildcard(permissionKey))
            {
                throw new ArgumentException(
                    $"Explicit permission '{permissionKey.Trim()}' must be a concrete permission key, not a wildcard pattern.",
                    nameof(permissionKeys));
            }

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
        var source = PermissionKey.Normalize(pagePath);
        var target = PermissionKey.Normalize(permissionKey);

        if (_permissionKeyOverrides.TryGetValue(source, out var existing))
        {
            if (!PermissionKey.Equals(existing, target))
            {
                throw new InvalidOperationException(
                    $"Permission override '{source}' already maps to '{existing}' and cannot also map to '{target}'.");
            }

            return this;
        }

        _permissionKeyOverrides.Add(source, target);
        return this;
    }

    public AutoPolicyOptions AddAlias(string alias, string canonicalKey)
    {
        var aliasKey = PermissionKey.Normalize(alias);
        var target = PermissionKey.Normalize(canonicalKey);

        if (PermissionKey.Equals(aliasKey, target))
        {
            throw new ArgumentException(
                $"Permission alias '{aliasKey}' cannot resolve to itself.",
                nameof(canonicalKey));
        }

        if (_aliases.TryGetValue(aliasKey, out var existing))
        {
            if (!PermissionKey.Equals(existing, target))
            {
                throw new InvalidOperationException(
                    $"Permission alias '{aliasKey}' already maps to '{existing}' and cannot also map to '{target}'.");
            }

            return this;
        }

        _aliases.Add(aliasKey, target);
        return this;
    }
}
