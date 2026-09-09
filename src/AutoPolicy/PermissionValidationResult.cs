namespace AutoPolicy;

internal sealed class PermissionValidationResult
{
    public static PermissionValidationResult Success { get; } = new();

    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    public IReadOnlyList<string> Errors => _errors;

    public IReadOnlyList<string> Warnings => _warnings;

    public bool IsValid => _errors.Count == 0;

    public void AddError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _errors.Add(message);
    }

    public void AddWarning(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _warnings.Add(message);
    }
}
