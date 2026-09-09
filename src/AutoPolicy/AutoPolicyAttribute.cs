namespace AutoPolicy;

/// <summary>
/// Opts a Razor Page into AutoPolicy protection using the permission derived from its canonical page identity.
/// </summary>
/// <remarks>
/// Pages are protected automatically by default, so this attribute is normally only needed when
/// <see cref="AutoPolicyOptions.RazorPagesProtectedByDefault"/> has been disabled.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AutoPolicyAttribute : Attribute
{
}
