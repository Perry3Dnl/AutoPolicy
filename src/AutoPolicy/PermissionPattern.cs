namespace AutoPolicy;

/// <summary>
/// Exact keys, trailing <c>/*</c> prefix rules, and a single <c>*</c> match-all pattern.
/// </summary>
public static class PermissionPattern
{
    public const string MatchAll = "*";

    public static bool IsWildcard(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Validate(pattern);

        var trimmed = pattern.Trim();
        return trimmed == MatchAll || trimmed.EndsWith("/*", StringComparison.Ordinal);
    }

    public static void Validate(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var trimmed = pattern.Trim();
        if (trimmed == MatchAll)
        {
            return;
        }

        var star = trimmed.IndexOf('*');
        if (star >= 0)
        {
            var isSingleTrailingWildcard =
                star == trimmed.Length - 1
                && trimmed.EndsWith("/*", StringComparison.Ordinal);

            if (!isSingleTrailingWildcard)
            {
                throw new ArgumentException(
                    $"Invalid permission pattern '{pattern}'. Only '*' and a single trailing '/*' wildcard are supported.",
                    nameof(pattern));
            }
        }

        if (trimmed.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = trimmed[..^2];
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    $"Invalid permission pattern '{pattern}'. A trailing '/*' rule requires a non-empty prefix.",
                    nameof(pattern));
            }

            _ = PermissionKey.Normalize(prefix);
            return;
        }

        _ = PermissionKey.Normalize(trimmed);
    }

    public static bool Matches(string permissionKey, string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        Validate(pattern);

        var key = PermissionKey.Normalize(permissionKey);
        var trimmed = pattern.Trim();

        if (trimmed == MatchAll)
        {
            return true;
        }

        if (trimmed.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = PermissionKey.Normalize(trimmed[..^2]);
            return key.Length > prefix.Length
                && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && key[prefix.Length] == '/';
        }

        return string.Equals(key, PermissionKey.Normalize(trimmed), StringComparison.OrdinalIgnoreCase);
    }
}
