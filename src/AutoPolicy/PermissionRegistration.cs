namespace AutoPolicy;

/// <summary>
/// Describes a canonical permission known to the AutoPolicy registry.
/// </summary>
public sealed class PermissionRegistration
{
    internal PermissionRegistration(
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

    /// <summary>
    /// Gets the canonical permission key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the Razor Page view-engine path when the permission was discovered from a page.
    /// </summary>
    public string? ViewEnginePath { get; }

    /// <summary>
    /// Gets the Razor Pages area name when applicable.
    /// </summary>
    public string? Area { get; }

    /// <summary>
    /// Gets the Razor Page relative path when the permission was discovered from a page.
    /// </summary>
    public string? RelativePath { get; }

    /// <summary>
    /// Gets whether AutoPolicy automatically protects the discovered Razor Page.
    /// </summary>
    public bool IsProtected { get; }
}
