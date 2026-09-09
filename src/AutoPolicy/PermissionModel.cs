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

        var normalizedName = name.Trim();
        if (_groups.ContainsKey(normalizedName))
        {
            throw new InvalidOperationException(
                $"Permission group '{normalizedName}' has already been defined.");
        }

        var builder = new PermissionGroupBuilder(normalizedName);
        configure(builder);
        var definition = builder.Build();
        _groups.Add(definition.Name, definition);
        return this;
    }

    public PermissionModel DefineRole(string name, Action<PermissionRoleBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var normalizedName = name.Trim();
        if (_roles.ContainsKey(normalizedName))
        {
            throw new InvalidOperationException(
                $"Permission role '{normalizedName}' has already been defined.");
        }

        var builder = new PermissionRoleBuilder(normalizedName);
        configure(builder);
        var definition = builder.Build();
        _roles.Add(definition.Name, definition);
        return this;
    }

    public bool TryGetGroup(string name, out PermissionGroupDefinition group) =>
        _groups.TryGetValue(name, out group!);

    public bool TryGetRole(string name, out PermissionRoleDefinition role) =>
        _roles.TryGetValue(name, out role!);
}
