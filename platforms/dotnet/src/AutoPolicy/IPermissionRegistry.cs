namespace AutoPolicy;

public interface IPermissionRegistry
{
    IReadOnlyCollection<string> Keys { get; }

    bool Contains(string permissionKey);

    IReadOnlyList<PermissionRegistration> GetAll();
}
