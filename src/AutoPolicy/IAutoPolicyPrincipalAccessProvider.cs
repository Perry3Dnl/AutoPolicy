using System.Security.Claims;

namespace AutoPolicy;

/// <summary>
/// Supplies AutoPolicy access for authorization that runs without an active HTTP request,
/// such as interactive server-side Blazor component routing.
/// </summary>
/// <remarks>
/// Implement this interface when a custom <see cref="IAutoPolicyAccessProvider"/> must also support
/// authorization contexts that expose only a <see cref="ClaimsPrincipal"/>. AutoPolicy fails closed
/// for those contexts when the configured provider doesn't implement this interface.
/// </remarks>
public interface IAutoPolicyPrincipalAccessProvider
{
    /// <summary>
    /// Gets the access rules that apply to the supplied principal.
    /// </summary>
    /// <param name="user">The principal being authorized.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns>The access snapshot to evaluate.</returns>
    ValueTask<AutoPolicyAccess> GetAccessAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
