namespace AutoPolicy;

internal sealed class AutoPolicyMetadata
{
    /// <summary>
    /// Creates metadata for a permission, or a route-derived permission when the key is null.
    /// </summary>
    public AutoPolicyMetadata(string? permissionKey = null, bool isProtected = true)
    {
        Key = permissionKey is null
            ? null
            : AutoPolicy.PermissionKey.Normalize(permissionKey);
        IsProtected = isProtected;
    }

    /// <summary>
    /// Gets the explicit permission key, or null to derive it from the endpoint route.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Gets whether AutoPolicy protects this endpoint according to Razor Page configuration.
    /// </summary>
    public bool IsProtected { get; }
}
