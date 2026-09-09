using Microsoft.AspNetCore.Http;

namespace AutoPolicy;

/// <summary>
/// Supplies the access snapshot that applies to the current HTTP request.
/// </summary>
/// <remarks>
/// This is the integration boundary between AutoPolicy and the host application's identity and
/// permission storage model. The application owns user accounts, authentication, persistence,
/// role/group assignment, and administrative permission editing. AutoPolicy only consumes the
/// resulting <see cref="AutoPolicyAccess"/> snapshot and evaluates it.
/// </remarks>
public interface IAutoPolicyAccessProvider
{
    /// <summary>
    /// Gets the access rules that apply to the supplied request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns>
    /// The access snapshot to evaluate. Return <see cref="AutoPolicyAccess.Empty"/> when no access
    /// information is available; do not return <see langword="null"/>.
    /// </returns>
    ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
