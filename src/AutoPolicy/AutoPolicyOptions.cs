namespace AutoPolicy;

public sealed class AutoPolicyOptions
{
    public PermissionModel Model { get; } = new();

    public bool RazorPagesProtectedByDefault { get; set; } = true;

    public bool StrictValidation { get; set; }

    public IList<string> AnonymousPatterns { get; } = new List<string>();

    public IDictionary<string, string> PermissionKeyOverrides { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, string> Aliases { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public AutoPolicyOptions ProtectRazorPagesByDefault(bool enabled = true)
    {
        RazorPagesProtectedByDefault = enabled;
        return this;
    }

    public AutoPolicyOptions AllowAnonymous(params string[] patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        foreach (var pattern in patterns)
        {
            PermissionPattern.Validate(pattern);
            AnonymousPatterns.Add(pattern.Trim());
        }

        return this;
    }

    public AutoPolicyOptions DefineGroup(string name, Action<PermissionGroupBuilder> configure)
    {
        Model.DefineGroup(name, configure);
        return this;
    }

    public AutoPolicyOptions DefineRole(string name, Action<PermissionRoleBuilder> configure)
    {
        Model.DefineRole(name, configure);
        return this;
    }

    public AutoPolicyOptions OverridePermissionKey(string pagePath, string permissionKey)
    {
        PermissionKeyOverrides[PermissionKey.Normalize(pagePath)] = PermissionKey.Normalize(permissionKey);
        return this;
    }

    public AutoPolicyOptions AddAlias(string alias, string canonicalKey)
    {
        Aliases[PermissionKey.Normalize(alias)] = PermissionKey.Normalize(canonicalKey);
        return this;
    }
}
