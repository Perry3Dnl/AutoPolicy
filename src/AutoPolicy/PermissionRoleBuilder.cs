namespace AutoPolicy;

public sealed class PermissionRoleBuilder
{
    internal PermissionRoleBuilder(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal List<string> Patterns { get; } = [];

    internal List<string> IncludedGroups { get; } = [];

    public PermissionRoleBuilder Include(string pattern)
    {
        PermissionPattern.Validate(pattern);
        Patterns.Add(pattern.Trim());
        return this;
    }

    public PermissionRoleBuilder IncludeGroup(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        IncludedGroups.Add(groupName.Trim());
        return this;
    }

    internal PermissionRoleDefinition Build() =>
        new(Name, Patterns.ToArray(), IncludedGroups.ToArray());
}
