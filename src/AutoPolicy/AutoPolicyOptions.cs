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

    /// <summary>
    /// Creates the default AutoPolicy configuration.
    /// </summary>
    public AutoPolicyOptions()
    {
        _anonymousPatternsView = _anonymousPatterns.AsReadOnly();
        _permissionKeyOverridesView = new ReadOnlyDictionary<string, string>(_permissionKeyOverrides);
        _aliasesView = new ReadOnlyDictionary<string, string>(_aliases);
    }

    internal PermissionModel Model { get; } = new();

    /// <summary>
    /// Gets or sets whether discovered Razor Pages are protected automatically. The default is <see langword="true"/>.
    /// </summary>
    public bool RazorPagesProtectedByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets whether stale exact permission references and wildcard patterns that match no registered permission
    /// are startup errors instead of warnings.
    /// </summary>
    public bool StrictValidation { get; set; }

    /// <summary>
    /// Gets or sets how an AutoPolicy forbid is surfaced. The default delegates to the host
    /// application's normal ASP.NET Core authorization behavior.
    /// </summary>
    public PermissionDeniedBehavior PermissionDeniedBehavior { get; set; } = PermissionDeniedBehavior.Default;

    internal IReadOnlyList<string> AnonymousPatterns => _anonymousPatternsView;

    internal IReadOnlyCollection<string> ExplicitPermissions => _explicitPermissions;

    internal IReadOnlyDictionary<string, string> PermissionKeyOverrides => _permissionKeyOverridesView;

    internal IReadOnlyDictionary<string, string> Aliases => _aliasesView;

    /// <summary>
    /// Enables or disables automatic protection for discovered Razor Pages.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to protect pages by default; otherwise <see langword="false"/>.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
    public AutoPolicyOptions ProtectRazorPagesByDefault(bool enabled = true)
    {
        RazorPagesProtectedByDefault = enabled;
        return this;
    }

    /// <summary>
    /// Marks one or more exact permission keys or trailing <c>/*</c> permission prefixes as anonymous.
    /// </summary>
    /// <remarks>
    /// The bare <c>*</c> match-all pattern is intentionally rejected. Use
    /// <see cref="ProtectRazorPagesByDefault(bool)"/> for an intentional global opt-out.
    /// Standard ASP.NET Core <c>[AllowAnonymous]</c> metadata is also respected.
    /// </remarks>
    /// <param name="patterns">The anonymous permission patterns.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
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
    /// Registers one or more concrete permissions that are not discovered from Razor Page identities.
    /// </summary>
    /// <remarks>
    /// Use this for partials, page sections, buttons, menu items, and application capabilities that
    /// require a permission identity of their own. Explicit permissions participate in roles,
    /// groups, wildcard matching, deny-wins evaluation, registry diagnostics, and in-page checks
    /// exactly like discovered page permissions. Wildcards are not valid permission identities here.
    /// </remarks>
    /// <param name="permissionKeys">Concrete permission keys to register.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
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

    /// <summary>
    /// Defines a named permission group that can contain permission patterns and other groups.
    /// </summary>
    /// <param name="name">The application-defined group name.</param>
    /// <param name="configure">Configures the permissions included by the group.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
    public AutoPolicyOptions DefineGroup(string name, Action<PermissionGroupBuilder> configure)
    {
        Model.DefineGroup(name, configure);
        return this;
    }

    /// <summary>
    /// Defines a named role that can contain permission patterns and groups.
    /// </summary>
    /// <param name="name">The application-defined role name.</param>
    /// <param name="configure">Configures the permissions included by the role.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
    public AutoPolicyOptions DefineRole(string name, Action<PermissionRoleBuilder> configure)
    {
        Model.DefineRole(name, configure);
        return this;
    }

    /// <summary>
    /// Replaces the automatically derived permission key for one discovered Razor Page identity.
    /// </summary>
    /// <remarks>
    /// The source is the canonical Razor Page identity, not a runtime URL or route value. Startup validation
    /// fails if the source page does not exist or if the resulting key conflicts with another registration.
    /// </remarks>
    /// <param name="pagePath">The canonical discovered page identity to remap.</param>
    /// <param name="permissionKey">The concrete permission key the page should require.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
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

    /// <summary>
    /// Adds an alternate or legacy permission key that resolves to a canonical registered permission.
    /// </summary>
    /// <remarks>
    /// Alias chains are supported. Cycles, aliases that shadow registered permissions, and aliases whose final
    /// target is not registered fail startup validation.
    /// </remarks>
    /// <param name="alias">The alternate permission key.</param>
    /// <param name="canonicalKey">The permission key the alias should resolve to.</param>
    /// <returns>The same options instance for fluent configuration.</returns>
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
