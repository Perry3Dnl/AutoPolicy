# AutoPolicy

**Automatic, default-deny route permissions for ASP.NET Core Razor Pages.**

AutoPolicy discovers Razor Pages, maps them to stable permission identities, and evaluates access through ASP.NET Core authorization. Normal pages require no AutoPolicy attributes, policy strings, custom base classes, or injected permission services.

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
