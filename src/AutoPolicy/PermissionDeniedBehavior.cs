namespace AutoPolicy;

/// <summary>
/// Controls how AutoPolicy authorization forbids are surfaced by the ASP.NET Core authorization middleware.
/// </summary>
public enum PermissionDeniedBehavior
{
    /// <summary>
    /// Uses the application's normal ASP.NET Core authorization behavior.
    /// Cookie authentication may redirect to an access-denied page, while API schemes commonly return 403.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Returns HTTP 403 for AutoPolicy forbids instead of delegating the forbid to the authentication handler.
    /// Authentication challenges still use the application's normal behavior.
    /// </summary>
    StatusCode403 = 1
}
