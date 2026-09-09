namespace AutoPolicy;

/// <summary>
/// Canonical permission identity derived from a page or endpoint definition.
/// Keys use a leading slash, no trailing slash, and ordinal ignore-case comparison.
/// Query strings and fragments are not part of the identity.
/// </summary>
public readonly struct PermissionKey : IEquatable<PermissionKey>
{
    public PermissionKey(string value)
    {
        Value = Normalize(value);
    }

    public string Value { get; }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Permission key cannot be empty.", nameof(value));
        }

        var s = value.Trim();

        var terminator = s.IndexOfAny(['?', '#']);
        if (terminator >= 0)
        {
            s = s[..terminator];
        }

        s = s.Replace('\\', '/');

        while (s.Contains("//", StringComparison.Ordinal))
        {
            s = s.Replace("//", "/", StringComparison.Ordinal);
        }

        if (s.Length == 0)
        {
            throw new ArgumentException("Permission key cannot be empty.", nameof(value));
        }

        if (s.Contains('*'))
        {
            throw new ArgumentException(
                "Permission key cannot contain wildcard characters. Use PermissionPattern for wildcard rules.",
                nameof(value));
        }

        if (s[0] != '/')
        {
            s = "/" + s;
        }

        if (s.Length > 1)
        {
            s = s.TrimEnd('/');
        }

        return s;
    }

    public static bool Equals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(PermissionKey other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is PermissionKey other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(PermissionKey left, PermissionKey right) => left.Equals(right);

    public static bool operator !=(PermissionKey left, PermissionKey right) => !left.Equals(right);

    public static implicit operator string(PermissionKey key) => key.Value;
}
