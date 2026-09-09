namespace AutoPolicy;

/// <summary>
/// Resolves a permission key to its final canonical identity, including alias chains.
/// </summary>
internal static class AutoPolicyPermissionKeyResolver
{
    public static string Resolve(string permissionKey, AutoPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var current = PermissionKey.Normalize(permissionKey);
        if (options.Aliases.Count == 0)
        {
            return current;
        }

        HashSet<string>? visited = null;

        while (options.Aliases.TryGetValue(current, out var next))
        {
            visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    $"Permission alias cycle detected while resolving '{PermissionKey.Normalize(permissionKey)}'.");
            }

            current = PermissionKey.Normalize(next);
        }

        return current;
    }
}
