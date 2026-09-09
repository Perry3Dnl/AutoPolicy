namespace AutoPolicy;

public sealed class PermissionGroupBuilder
{
    internal PermissionGroupBuilder(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal List<string> Patterns { get; } = [];

    internal List<string> IncludedGroups { get; } = [];

    public PermissionGroupBuilder Include(string pattern)
    {
        PermissionPattern.Validate(pattern);
        Patterns.Add(pattern.Trim());
        return this;
    }

    public PermissionGroupBuilder IncludeGroup(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        IncludedGroups.Add(groupName.Trim());
        return this;
    }

    internal PermissionGroupDefinition Build() =>
        new(Name, Patterns.ToArray(), IncludedGroups.ToArray());
}
