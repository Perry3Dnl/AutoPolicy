namespace AutoPolicy;

/// <summary>
/// Application-supplied allow and deny lists for the current user.
/// Deny entries win over allows after role and group expansion.
/// </summary>
public sealed class UserAccess
{
    public static UserAccess Empty { get; } = new();

    public IReadOnlyCollection<string> AllowRoles { get; init; } = [];

    public IReadOnlyCollection<string> AllowGroups { get; init; } = [];

    public IReadOnlyCollection<string> AllowPermissions { get; init; } = [];

    public IReadOnlyCollection<string> DenyRoles { get; init; } = [];

    public IReadOnlyCollection<string> DenyGroups { get; init; } = [];

    public IReadOnlyCollection<string> DenyPermissions { get; init; } = [];

    public bool IsEmpty =>
        AllowRoles.Count == 0
        && AllowGroups.Count == 0
        && AllowPermissions.Count == 0
        && DenyRoles.Count == 0
        && DenyGroups.Count == 0
        && DenyPermissions.Count == 0;
}
