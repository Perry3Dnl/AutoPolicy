# AutoPolicy

**Automatic, default-deny permissions for ASP.NET Core routes and server-rendered applications.**

AutoPolicy discovers Razor Pages and protects endpoint-routed requests by default, including MVC, Minimal APIs, gRPC RPCs, mapped static assets, SignalR connections, and hub methods. Normal endpoints need no AutoPolicy attributes or per-endpoint calls.

## Install

```bash
dotnet add package AutoPolicy --version 0.1.2
```

AutoPolicy `0.1.2` targets .NET 10.

## Basic setup

```csharp
builder.Services.AddRazorPages();

builder.Services.AddAutoPolicy(options =>
{
    options.AllowAnonymous("/Account/Login", "/Error");

    options.DefineGroup("StaffPages", group =>
        group.Include("/Staff/*"));

    options.DefineRole("Staff", role =>
        role.IncludeGroup("StaffPages"));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

Razor Pages are protected by default. With no grants, protected pages are denied.

A page at:

```text
Pages/Staff/Members/Detail.cshtml
```

receives the canonical permission:

```text
/Staff/Members/Detail
```

Custom `@page` templates, route values, query strings, and fragments do not change that identity.

## ASP.NET Core endpoints and Blazor

All endpoint-routed requests that reach `UseAuthorization()` are protected by default, including MVC, Razor Pages, Minimal APIs, gRPC RPCs, mapped static assets, and SignalR connection endpoints. Unmarked endpoints are denied unless the user has the route permission. HTTP method and port aren't part of endpoint permission keys. Use `.AllowAnonymous()` for public endpoints. `.RequireAutoPolicy("...")` is optional when you want a capability key instead of the route pattern.

SignalR hub method calls are also checked by default. The hub connection endpoint and each hub method use separate permission keys.

Interactive server-side Blazor component navigation uses the app's router. Add `@attribute [Authorize]` to the app's `_Imports.razor`, then use `<AuthorizeRouteView Resource="routeData" RouteData="routeData" />` so AutoPolicy can map each routed component to its route permission. Public components can use `[AllowAnonymous]`. Custom access providers that support component routing should implement `IAutoPolicyPrincipalAccessProvider`.

Blazor WebAssembly client-side authorization is a UI feature; protect the server API that provides data and performs operations. Static files mapped with `MapStaticAssets()` are protected by default. Static files served by `UseStaticFiles()` before authorization middleware bypass endpoint fallback policy.

See [endpoint authorization](https://github.com/Perry3Dnl/AutoPolicy/blob/main/docs/MINIMAL_API.md) for setup details.

## Bring your own permission storage

AutoPolicy does not manage users, accounts, persistence, or permission assignments. Implement `IAutoPolicyAccessProvider` to integrate an existing permission system:

```csharp
public sealed class ApplicationAccessProvider : IAutoPolicyAccessProvider
{
    public async ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var stored = await LoadPermissionsAsync(context, cancellationToken);
        if (stored is null)
        {
            return AutoPolicyAccess.Empty;
        }

        return new AutoPolicyAccess
        {
            AllowRoles = stored.AllowRoles,
            DenyRoles = stored.DenyRoles,
            AllowGroups = stored.AllowGroups,
            DenyGroups = stored.DenyGroups,
            AllowPermissions = stored.AllowPermissions,
            DenyPermissions = stored.DenyPermissions
        };
    }
}
```

Register it before AutoPolicy:

```csharp
builder.Services.AddScoped<IAutoPolicyAccessProvider, ApplicationAccessProvider>();
builder.Services.AddAutoPolicy();
```

The access snapshot is cached once per HTTP request.

## Roles, groups, and deny-wins

```csharp
builder.Services.AddAutoPolicy(options =>
{
    options.DefinePermission(
        "/Administration/Accounts/Permissions/EditRoles");

    options.DefineGroup("PermissionEditors", group =>
        group.Include("/Administration/Accounts/Permissions/EditRoles"));

    options.DefineRole("Admin", role =>
    {
        role.IncludeGroup("PermissionEditors");
        role.Include("/Reports/*");
    });
});
```

The host application returns role/group/direct assignments through `AutoPolicyAccess`. Any matching deny overrides every matching allow.

## Wildcards

Supported permission patterns are intentionally limited:

```text
/Admin/Users    exact
/Admin/*        descendants of /Admin
*               every registered permission
```

`/Admin/*` does not match `/Admin`, `/Administrator`, or `/Admin2/Users`. Embedded wildcards such as `/Admin/*/Edit`, `/Admin/**`, and `/Admin*` are rejected.

`*` only applies to permissions already present in the registry; it cannot invent an unknown capability.

## Partials and in-page permissions

Register non-route capabilities for partials, panels, menu items, buttons, tabs, or operations:

```csharp
options.DefinePermission(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/ViewHistory");
```

Then check them from Razor:

```razor
@using AutoPolicy

@{
    var canEditRoles = await Model.HasAccessAsync(
        "/Administration/Accounts/Permissions/EditRoles");
}
```

Or:

```csharp
var canManageAnything = await HttpContext.HasAnyAccessAsync(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/ViewHistory");
```

Unknown in-page permission keys fail closed, even when a broad wildcard would otherwise match.

## Anonymous pages

Standard ASP.NET Core `[AllowAnonymous]` is respected. You can also configure exact or prefix rules:

```csharp
options.AllowAnonymous("/Account/Login", "/Public/*");
```

Bare `AllowAnonymous("*")` is rejected. For an intentional opt-in model:

```csharp
options.ProtectRazorPagesByDefault(false);
```

Then add `[AutoPolicy]` to specific PageModels that should be protected.

## Permission-denied behavior

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

uses the application's normal ASP.NET Core behavior.

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

returns a direct HTTP 403 for AutoPolicy forbids while preserving normal authentication challenges and unrelated authorization policies.

## Built-in claims adapter

Without a custom provider, authenticated claims are mapped using these defaults:

| Access | Claim type |
| --- | --- |
| Allow roles | `ClaimTypes.Role` |
| Allow groups | `permission_group` |
| Allow permissions | `permission_allow` |
| Deny roles | `permission_deny_role` |
| Deny groups | `permission_deny_group` |
| Deny permissions | `permission_deny` |

## Validation and diagnostics

`IPermissionRegistry` exposes discovered and explicitly registered permissions for diagnostics. Startup validation rejects duplicate canonical keys, cyclic/missing groups, invalid aliases, conflicting page overrides, and other structural errors. Set `StrictValidation = true` to turn stale permission references and wildcard patterns that match nothing into startup errors.

AutoPolicy fails closed when permission state is missing or invalid, and deny rules always win.

## Links

- [GitHub repository](https://github.com/Perry3Dnl/AutoPolicy)
- [Authorization design](https://github.com/Perry3Dnl/AutoPolicy/blob/main/docs/AUTHORIZATION.md)
- [Changelog](https://github.com/Perry3Dnl/AutoPolicy/blob/main/CHANGELOG.md)

## License

Mozilla Public License 2.0 (`MPL-2.0`).
