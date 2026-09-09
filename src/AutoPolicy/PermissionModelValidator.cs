namespace AutoPolicy;

public static class PermissionModelValidator
{
    public static PermissionValidationResult Validate(
        PermissionModel model,
        IPermissionRegistry registry,
        IEnumerable<string>? additionalExplicitKeys = null,
        bool strict = false)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(registry);

        var result = new PermissionValidationResult();

        if (registry is PermissionRegistry concrete)
        {
            foreach (var duplicate in concrete.Duplicates)
            {
                result.AddError($"Duplicate canonical permission key '{duplicate.Key}' ({duplicate.Source}).");
            }
        }

        var keys = new HashSet<string>(registry.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var group in model.Groups.Values)
        {
            ValidatePatterns(group.Name, "group", group.Patterns, keys, strict, result);

            foreach (var included in group.IncludedGroups)
            {
                if (!model.TryGetGroup(included, out _))
                {
                    result.AddError($"Group '{group.Name}' references unknown group '{included}'.");
                }
            }

            DetectCycle(model, group.Name, result);
        }

        foreach (var role in model.Roles.Values)
        {
            ValidatePatterns(role.Name, "role", role.Patterns, keys, strict, result);

            foreach (var included in role.IncludedGroups)
            {
                if (!model.TryGetGroup(included, out _))
                {
                    result.AddError($"Role '{role.Name}' references unknown group '{included}'.");
                }
            }
        }

        if (additionalExplicitKeys is not null)
        {
            foreach (var key in additionalExplicitKeys)
            {
                ReportStale("explicit permission", key, keys, strict, result);
            }
        }

        return result;
    }

    private static void ValidatePatterns(
        string ownerName,
        string ownerKind,
        IReadOnlyList<string> patterns,
        HashSet<string> keys,
        bool strict,
        PermissionValidationResult result)
    {
        foreach (var pattern in patterns)
        {
            try
            {
                PermissionPattern.Validate(pattern);
            }
            catch (ArgumentException ex)
            {
                result.AddError($"{ownerKind} '{ownerName}': {ex.Message}");
                continue;
            }

            if (PermissionPattern.IsWildcard(pattern))
            {
                if (pattern != PermissionPattern.MatchAll && !keys.Any(key => PermissionPattern.Matches(key, pattern)))
                {
                    var message = $"{ownerKind} '{ownerName}' pattern '{pattern}' does not match any registered permission.";
                    if (strict)
                    {
                        result.AddError(message);
                    }
                    else
                    {
                        result.AddWarning(message);
                    }
                }

                continue;
            }

            ReportStale($"{ownerKind} '{ownerName}'", pattern, keys, strict, result);
        }
    }

    private static void ReportStale(
        string owner,
        string key,
        HashSet<string> keys,
        bool strict,
        PermissionValidationResult result)
    {
        string normalized;
        try
        {
            normalized = PermissionKey.Normalize(key);
        }
        catch (ArgumentException ex)
        {
            result.AddError($"{owner}: {ex.Message}");
            return;
        }

        if (keys.Contains(normalized))
        {
            return;
        }

        var message = $"{owner} references stale permission '{normalized}' that does not map to a registered permission.";
        if (strict)
        {
            result.AddError(message);
        }
        else
        {
            result.AddWarning(message);
        }
    }

    private static void DetectCycle(PermissionModel model, string startGroup, PermissionValidationResult result)
    {
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Walk(string name)
        {
            if (!model.TryGetGroup(name, out var group))
            {
                return;
            }

            if (!visiting.Add(group.Name))
            {
                result.AddError(
                    $"Permission group '{startGroup}' contains a cyclic IncludeGroup reference involving '{group.Name}'.");
                return;
            }

            foreach (var included in group.IncludedGroups)
            {
                Walk(included);
            }

            visiting.Remove(group.Name);
        }

        Walk(startGroup);
    }
}
