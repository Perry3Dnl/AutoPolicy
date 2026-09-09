namespace AutoPolicy;

/// <summary>
/// Describes the allow and deny rules that apply to the current request.
/// </summary>
/// <remarks>
/// AutoPolicy evaluates this snapshot independently of how the application identified the caller.
/// Deny entries always take precedence after role and group expansion.
/// </remarks>
public sealed class AutoPolicyAccess
{
    /// <summary>
    /// Gets an access snapshot containing no grants or denies.
    /// </summary>
    public static AutoPolicyAccess Empty { get; } = new();

    /// <summary>
    /// Gets roles that grant access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> AllowRoles { get; init; } = [];

    /// <summary>
    /// Gets groups that grant access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> AllowGroups { get; init; } = [];

    /// <summary>
    /// Gets direct permission patterns that grant access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> AllowPermissions { get; init; } = [];

    /// <summary>
    /// Gets roles that explicitly deny access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> DenyRoles { get; init; } = [];

    /// <summary>
    /// Gets groups that explicitly deny access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> DenyGroups { get; init; } = [];

    /// <summary>
    /// Gets direct permission patterns that explicitly deny access for the current request.
    /// </summary>
    public IReadOnlyCollection<string> DenyPermissions { get; init; } = [];

    /// <summary>
    /// Gets whether the snapshot contains no allow or deny entries.
    /// </summary>
    public bool IsEmpty =>
        AllowRoles.Count == 0
        && AllowGroups.Count == 0
        && AllowPermissions.Count == 0
        && DenyRoles.Count == 0
        && DenyGroups.Count == 0
        && DenyPermissions.Count == 0;
}
