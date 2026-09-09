namespace AutoPolicy;

public sealed class PermissionEvaluator : IPermissionEvaluator
{
    private readonly PermissionModel _model;

    public PermissionEvaluator(PermissionModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public bool HasAccess(string requiredPermission, UserAccess access)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPermission);
        ArgumentNullException.ThrowIfNull(access);

        var key = PermissionKey.Normalize(requiredPermission);
        var denied = ExpandPatterns(access, denied: true);
        if (denied.Any(pattern => PermissionPattern.Matches(key, pattern)))
        {
            return false;
        }

        var allowed = ExpandPatterns(access, denied: false);
        return allowed.Any(pattern => PermissionPattern.Matches(key, pattern));
    }

    public IReadOnlyCollection<string> Expand(UserAccess access, bool denied)
    {
        ArgumentNullException.ThrowIfNull(access);
        return ExpandPatterns(access, denied);
    }

    public IReadOnlyCollection<string> GetEffectivePermissions(UserAccess access, IPermissionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(registry);

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in ExpandPatterns(access, denied: false))
        {
            foreach (var key in ExpandPatternAgainstRegistry(pattern, registry))
            {
                allowed.Add(key);
            }
        }

        foreach (var pattern in ExpandPatterns(access, denied: true))
        {
            foreach (var key in ExpandPatternAgainstRegistry(pattern, registry))
            {
                allowed.Remove(key);
            }
        }

        return allowed;
    }

    private IReadOnlyCollection<string> ExpandPatterns(UserAccess access, bool denied)
    {
        var roles = denied ? access.DenyRoles : access.AllowRoles;
        var groups = denied ? access.DenyGroups : access.AllowGroups;
        var permissions = denied ? access.DenyPermissions : access.AllowPermissions;

        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                continue;
            }

            PermissionPattern.Validate(permission);
            patterns.Add(permission.Trim());
        }

        foreach (var groupName in groups)
        {
            ExpandGroup(groupName, patterns, []);
        }

        foreach (var roleName in roles)
        {
            ExpandRole(roleName, patterns, []);
        }

        return patterns;
    }

    private void ExpandRole(string roleName, ISet<string> patterns, HashSet<string> stack)
    {
        if (string.IsNullOrWhiteSpace(roleName) || !_model.TryGetRole(roleName, out var role))
        {
            return;
        }

        foreach (var pattern in role.Patterns)
        {
            patterns.Add(pattern);
        }

        foreach (var groupName in role.IncludedGroups)
        {
            ExpandGroup(groupName, patterns, stack);
        }
    }

    private void ExpandGroup(string groupName, ISet<string> patterns, HashSet<string> stack)
    {
        if (string.IsNullOrWhiteSpace(groupName) || !_model.TryGetGroup(groupName, out var group))
        {
            return;
        }

        if (!stack.Add(group.Name))
        {
            throw new InvalidOperationException(
                $"Permission group '{group.Name}' contains a cyclic IncludeGroup reference.");
        }

        foreach (var pattern in group.Patterns)
        {
            patterns.Add(pattern);
        }

        foreach (var included in group.IncludedGroups)
        {
            ExpandGroup(included, patterns, stack);
        }

        stack.Remove(group.Name);
    }

    private static IEnumerable<string> ExpandPatternAgainstRegistry(string pattern, IPermissionRegistry registry)
    {
        if (!PermissionPattern.IsWildcard(pattern))
        {
            yield return PermissionKey.Normalize(pattern);
            yield break;
        }

        foreach (var registration in registry.GetAll())
        {
            if (PermissionPattern.Matches(registration.Key, pattern))
            {
                yield return registration.Key;
            }
        }
    }
}
