namespace AutoPolicy;

public sealed class PermissionRegistration
{
    public PermissionRegistration(
        string key,
        string? viewEnginePath = null,
        string? area = null,
        string? relativePath = null,
        bool isProtected = false)
    {
        Key = PermissionKey.Normalize(key);
        ViewEnginePath = viewEnginePath;
        Area = area;
        RelativePath = relativePath;
        IsProtected = isProtected;
    }

    public string Key { get; }

    public string? ViewEnginePath { get; }

    public string? Area { get; }

    public string? RelativePath { get; }

    public bool IsProtected { get; }
}
