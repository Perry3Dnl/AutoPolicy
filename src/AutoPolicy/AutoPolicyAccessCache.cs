using Microsoft.AspNetCore.Http;

namespace AutoPolicy;

/// <summary>
/// Caches the access snapshot returned by the configured provider for the lifetime of an HTTP request.
/// </summary>
internal static class AutoPolicyAccessCache
{
    private static readonly object CacheKey = new();

    public static ValueTask<AutoPolicyAccess> GetAsync(
        HttpContext context,
        IAutoPolicyAccessProvider provider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(provider);

        Task<AutoPolicyAccess> accessTask;

        lock (context.Items)
        {
            if (context.Items.TryGetValue(CacheKey, out var cached)
                && cached is Task<AutoPolicyAccess> cachedTask)
            {
                accessTask = cachedTask;
            }
            else
            {
                accessTask = LoadAsync(context, provider, cancellationToken);
                context.Items[CacheKey] = accessTask;
            }
        }

        return accessTask.IsCompletedSuccessfully
            ? ValueTask.FromResult(accessTask.Result)
            : new ValueTask<AutoPolicyAccess>(accessTask);
    }

    private static async Task<AutoPolicyAccess> LoadAsync(
        HttpContext context,
        IAutoPolicyAccessProvider provider,
        CancellationToken cancellationToken)
    {
        var access = await provider
            .GetAccessAsync(context, cancellationToken)
            .ConfigureAwait(false);

        return access
            ?? throw new InvalidOperationException(
                "IAutoPolicyAccessProvider returned null instead of an AutoPolicyAccess snapshot.");
    }
}
