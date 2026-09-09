namespace AutoPolicy;

public interface IPermissionEvaluator
{
    bool HasAccess(string requiredPermission, UserAccess access);

    IReadOnlyCollection<string> Expand(UserAccess access, bool denied);

    IReadOnlyCollection<string> GetEffectivePermissions(UserAccess access, IPermissionRegistry registry);
}
