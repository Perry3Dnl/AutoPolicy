namespace AutoPolicy;

/// <summary>
/// Builds an application-defined permission group.
/// </summary>
public sealed class PermissionGroupBuilder
{
    internal PermissionGroupBuilder(string name)
    {
        Name = name;
    }

    internal string Name { get; }

    internal List<string> Patterns { get; } = [];

    internal List<string> IncludedGroups { get; } = [];

    /// <summary>
    /// Adds an exact permission key, a trailing <c>/*</c> prefix pattern, or the <c>*</c> match-all pattern.
    /// </summary>
    /// <param name="pattern">The permission pattern granted by the group.</param>
    /// <returns>The same builder for fluent configuration.</returns>
    public PermissionGroupBuilder Include(string pattern)
    {
        PermissionPattern.Validate(pattern);
        Patterns.Add(pattern.Trim());
        return this;
    }

    /// <summary>
    /// Includes all permissions contributed by another configured group.
    /// </summary>
    /// <param name="groupName">The name of the group to include.</param>
    /// <returns>The same builder for fluent configuration.</returns>
    public PermissionGroupBuilder IncludeGroup(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        IncludedGroups.Add(groupName.Trim());
        return this;
    }

    internal PermissionGroupDefinition Build() =>
        new(Name, Patterns.ToArray(), IncludedGroups.ToArray());
}
