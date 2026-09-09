namespace AutoPolicy;

internal sealed class PermissionModel
{
    private readonly Dictionary<string, PermissionGroupDefinition> _groups =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, PermissionRoleDefinition> _roles =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, PermissionGroupDefinition> Groups => _groups;

    public IReadOnlyDictionary<string, PermissionRoleDefinition> Roles => _roles;

    public PermissionModel DefineGroup(string name, Action<PermissionGroupBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PermissionGroupBuilder(name.Trim());
        configure(builder);
        var definition = builder.Build();
        _groups[definition.Name] = definition;
        return this;
    }

    public PermissionModel DefineRole(string name, Action<PermissionRoleBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PermissionRoleBuilder(name.Trim());
        configure(builder);
        var definition = builder.Build();
        _roles[definition.Name] = definition;
        return this;
    }

    public bool TryGetGroup(string name, out PermissionGroupDefinition group) =>
        _groups.TryGetValue(name, out group!);

    public bool TryGetRole(string name, out PermissionRoleDefinition role) =>
        _roles.TryGetValue(name, out role!);
}
