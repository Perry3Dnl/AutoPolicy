namespace AutoPolicy;

/// <summary>
/// Exposes the canonical permissions discovered or explicitly registered by AutoPolicy.
/// </summary>
/// <remarks>
/// The registry is intended for diagnostics, administration tooling, and permission discovery.
/// It does not assign permissions to users.
/// </remarks>
public interface IPermissionRegistry
{
    /// <summary>
    /// Gets all registered canonical permission keys.
    /// </summary>
    IReadOnlyCollection<string> Keys { get; }

    /// <summary>
    /// Determines whether a concrete permission key is registered.
    /// </summary>
    /// <param name="permissionKey">The permission key to check.</param>
    /// <returns><see langword="true"/> when the canonical key is registered.</returns>
    bool Contains(string permissionKey);

    /// <summary>
    /// Gets registration metadata for all known permissions.
    /// </summary>
    /// <returns>The current permission registrations.</returns>
    IReadOnlyList<PermissionRegistration> GetAll();
}
