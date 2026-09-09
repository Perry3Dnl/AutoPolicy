namespace AutoPolicy;

internal sealed class PermissionGroupDefinition
{
    public PermissionGroupDefinition(
        string name,
        IReadOnlyList<string> patterns,
        IReadOnlyList<string> includedGroups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Patterns = patterns;
        IncludedGroups = includedGroups;
    }

    public string Name { get; }

    public IReadOnlyList<string> Patterns { get; }

    public IReadOnlyList<string> IncludedGroups { get; }
}
