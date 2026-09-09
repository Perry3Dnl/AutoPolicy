namespace AutoPolicy;

public interface IPermissionEvaluator
{
    bool HasAccess(string requiredPermission, AutoPolicyAccess access);

    IReadOnlyCollection<string> Expand(AutoPolicyAccess access, bool denied);

    IReadOnlyCollection<string> GetEffectivePermissions(AutoPolicyAccess access, IPermissionRegistry registry);
}
