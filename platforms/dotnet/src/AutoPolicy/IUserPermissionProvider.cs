using System.Security.Claims;

namespace AutoPolicy;

public interface IUserPermissionProvider
{
    ValueTask<UserAccess> GetAccessAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
