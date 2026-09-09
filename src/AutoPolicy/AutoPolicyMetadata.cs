namespace AutoPolicy;

public sealed class AutoPolicyMetadata
{
    public AutoPolicyMetadata(string permissionKey)
    {
        Key = AutoPolicy.PermissionKey.Normalize(permissionKey);
    }

    public string Key { get; }
}
