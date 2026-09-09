using System.Security.Claims;

namespace AutoPolicy;

public sealed class EmptyUserPermissionProvider : IUserPermissionProvider
{
    public ValueTask<UserAccess> GetAccessAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        _ = user;
        _ = cancellationToken;
        return ValueTask.FromResult(UserAccess.Empty);
    }
}
