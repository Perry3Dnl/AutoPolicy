using Microsoft.AspNetCore.Http;

namespace AutoPolicy;

/// <summary>
/// Supplies the access snapshot that applies to the current HTTP request.
/// </summary>
public interface IAutoPolicyAccessProvider
{
    /// <summary>
    /// Gets the access rules that apply to the supplied request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="cancellationToken">A token that can cancel access resolution.</param>
    /// <returns>The access snapshot to evaluate.</returns>
    ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
