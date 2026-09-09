# AutoPolicy

Automatic, default-deny route permissions for ASP.NET Core Razor Pages.

AutoPolicy discovers Razor Pages, maps them to canonical permission identities, and evaluates access through ASP.NET Core's standard authorization pipeline. It supports roles, groups, direct grants and denies, explicit non-route capabilities, aliases, anonymous route patterns, and deny-wins evaluation.

## Basic setup

```csharp
builder.Services.AddRazorPages();

builder.Services.AddAutoPolicy(options =>
{
    options.AllowAnonymous("/Account/Login", "/Error");

    options.DefinePermission(
        "/Administration/Accounts/AccountPermissions/EditRoles");
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

Most Razor Pages require no AutoPolicy-specific code. Their permission identity is discovered automatically.

## Bring your own permission storage

AutoPolicy does not manage user accounts, persistence, or assignments. Applications integrate their existing permission system by implementing `IAutoPolicyAccessProvider` and returning an `AutoPolicyAccess` snapshot containing the current request's allow/deny roles, groups, and direct permissions.

The built-in claims provider is only the default adapter; custom providers can load access from session state, a database, Redis, tenant data, or any application-specific source.

## In-page permissions

Partials, panels, buttons, menu items, and operations can have explicit permissions that do not map to a full Razor Page:

```csharp
options.DefinePermission(
    "/Administration/Accounts/AccountPermissions/EditRoles");
```

Then use the same deny-wins evaluator from Razor:

```csharp
var canEditRoles = await Model.HasAccessAsync(
    "/Administration/Accounts/AccountPermissions/EditRoles");
```

Unknown in-page permission keys fail closed, and the access snapshot is loaded at most once per HTTP request.

## Permission-denied behavior

`PermissionDeniedBehavior.Default` delegates to the application's normal ASP.NET Core behavior. `PermissionDeniedBehavior.StatusCode403` returns HTTP 403 for AutoPolicy forbids while preserving normal authentication challenges.

## Package status

The public API is still being refined for the first `0.1.0` release. Review the repository changelog when adopting a development build.

## License

Mozilla Public License 2.0 (`MPL-2.0`).
