namespace AutoPolicy;

/// <summary>
/// Marks a Razor Page as requiring the permission derived from its page identity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AutoPolicyAttribute : Attribute
{
}
