namespace AutoPolicy;

internal sealed class AutoPolicyMetadata
{
    /// <summary>
    /// Creates metadata for a permission, or a route-derived permission when the key is null.
    /// </summary>
    public AutoPolicyMetadata(string? permissionKey = null)
    {
        Key = permissionKey is null
            ? null
            : AutoPolicy.PermissionKey.Normalize(permissionKey);
    }

    /// <summary>
    /// Gets the explicit permission key, or null to derive it from the endpoint route.
    /// </summary>
    public string? Key { get; }
}
