namespace AutoPolicy;

public sealed class PermissionRegistry : IPermissionRegistry
{
    private readonly Dictionary<string, PermissionRegistration> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<(string Key, string Source)> _duplicates = [];

    public IReadOnlyCollection<string> Keys => _entries.Keys;

    public IReadOnlyList<(string Key, string Source)> Duplicates => _duplicates;

    public bool Contains(string permissionKey) =>
        _entries.ContainsKey(PermissionKey.Normalize(permissionKey));

    public IReadOnlyList<PermissionRegistration> GetAll() => [.. _entries.Values];

    public bool TryAdd(PermissionRegistration registration, string source)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (_entries.TryGetValue(registration.Key, out var existing))
        {
            if (string.Equals(existing.RelativePath, registration.RelativePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.ViewEnginePath, registration.ViewEnginePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Area, registration.Area, StringComparison.OrdinalIgnoreCase))
            {
                _entries[registration.Key] = registration;
                return true;
            }

            _duplicates.Add((registration.Key, $"{existing.RelativePath ?? existing.ViewEnginePath} vs {source}"));
            return false;
        }

        _entries[registration.Key] = registration;
        return true;
    }

    public void Clear()
    {
        _entries.Clear();
        _duplicates.Clear();
    }
}
