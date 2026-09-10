<h1>
  <img src="assets/autopolicy-icon.svg" alt="AutoPolicy icon" width="48" align="absmiddle" />
  AutoPolicy
</h1>

[![build](https://github.com/Perry3Dnl/AutoPolicy/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Perry3Dnl/AutoPolicy/actions/workflows/dotnet.yml)
[![license](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![target](https://img.shields.io/badge/.NET-net10.0-512BD4.svg)](src/AutoPolicy/AutoPolicy.csproj)
[![status](https://img.shields.io/badge/status-active%20development-6f42c1.svg)](CHANGELOG.md)

**Automatic, default-deny Razor Pages permissions for ASP.NET Core.**

AutoPolicy discovers Razor Pages, gives them stable permission identities, and evaluates roles, groups, grants, and deny-wins overrides through the standard ASP.NET Core authorization pipeline. Normal pages need no AutoPolicy attributes, policy strings, custom base classes, or injected permission services.

[**Authorization design**](docs/AUTHORIZATION.md) · [**Changelog**](CHANGELOG.md)

## ✨ Features

- ✅ Automatic permission identities for discovered **Razor Pages**
- ✅ **Default-deny** protection for normal pages
- ✅ Roles, groups, direct grants, and explicit denies
- ✅ **Deny always wins**, regardless of the allow source
- ✅ Exact permissions plus deliberately small, predictable wildcard rules
- ✅ Custom application access through `IAutoPolicyAccessProvider`
- ✅ Built-in claims adapter when no custom provider is registered
- ✅ Explicit capabilities for partials, panels, buttons, and other non-route checks
- ✅ Permission aliases and page-key overrides
- ✅ Startup validation for malformed or conflicting policy configuration
- ✅ Fail-closed behavior when access resolution fails
- ✅ Uses the normal ASP.NET Core authentication and authorization pipeline

## 📦 Package

| Package | Target | Purpose |
| --- | --- | --- |
| `AutoPolicy` | `net10.0` | Razor Pages permission discovery, authorization, roles, groups, grants, denies, claims integration, and policy validation |

AutoPolicy is intentionally a **single package**. There is no separate core package or ASP.NET Core integration package.

The first NuGet release is still being prepared. For local package validation:

```bash
dotnet pack src/AutoPolicy/AutoPolicy.csproj --configuration Release --output artifacts
```

## 🚀 Quick start

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

Razor Pages are protected by default. If a protected page has no effective grant, access is denied.

## Why AutoPolicy

Traditional ASP.NET Core authorization often means repeating policy names, attributes, or permission checks across every page. That works, but it also creates another layer of identifiers that can drift away from the application structure.

AutoPolicy uses the page itself as the canonical permission identity:

```text
Pages/Staff/Members/Detail.cshtml
→ /Staff/Members/Detail

Areas/BackOffice/Pages/Dashboard.cshtml
→ /BackOffice/Dashboard
```

That gives the application one predictable permission namespace while still leaving authentication, user storage, role assignment, and administration screens under your control.

The model is deliberately defensive:

- pages are protected by default;
- permissions must exist before they can be granted;
- malformed patterns are rejected;
- unknown in-page permissions return `false`;
- denies override every allow source;
- provider failures deny access instead of silently granting it.

## Automatic page permissions

Permission identity comes from the Razor Page itself, not from the browser URL at runtime.

Custom `@page` route templates, route values, query strings, and fragments do not change the permission identity. Permission keys are normalized and compared case-insensitively.

For example:

```razor
@page "/members/{id:int}"
```

still resolves to the page's canonical permission key, so requests such as `/members/12`, `/members/900`, and `/members/12?tab=history` do not create separate permissions.

## Roles, groups, grants, and denies

Roles and groups are application-defined names. AutoPolicy defines what those names mean; your application decides which ones apply to the current request.

```csharp
builder.Services.AddAutoPolicy(options =>
{
    options.DefinePermission(
        "/Administration/Accounts/Permissions/EditRoles",
        "/Administration/Accounts/Permissions/EditGroups");

    options.DefineGroup("AccountPermissionEditors", group =>
    {
        group.Include("/Administration/Accounts/Permissions/EditRoles");
        group.Include("/Administration/Accounts/Permissions/EditGroups");
    });

    options.DefineRole("Admin", role =>
    {
        role.IncludeGroup("AccountPermissionEditors");
        role.Include("/Reports/*");
    });
});
```

The access provider can then resolve the current request to something like:

```csharp
new AutoPolicyAccess
{
    AllowRoles = ["Admin"],
    DenyPermissions = ["/Reports/Payroll"]
};
```

The explicit deny wins even though the `Admin` role grants the broader `/Reports/*` pattern.

## Application access provider

AutoPolicy defines and evaluates access. Your application still owns:

- authentication and user accounts;
- databases, sessions, claims, Redis, APIs, or other permission sources;
- assigning roles and groups;
- direct per-user grants and denies;
- administration screens that edit assignments.

`IAutoPolicyAccessProvider` is the integration boundary:

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

Register the provider before `AddAutoPolicy`:

```csharp
builder.Services.AddScoped<IAutoPolicyAccessProvider, ApplicationAccessProvider>();
builder.Services.AddAutoPolicy(options =>
{
    // permission definitions
});
```

The resolved `AutoPolicyAccess` snapshot is cached once per HTTP request, so route authorization and repeated in-page checks use the same access state.

## Wildcards

AutoPolicy intentionally supports a small wildcard grammar:

```text
/Admin/Users       exact permission
/Admin/*           descendants of /Admin
*                  every registered permission
```

`/Admin/*` matches `/Admin/Users` and `/Admin/Users/Edit`, but not `/Admin`, `/Administrator`, or `/Admin2/Users`.

Embedded or ambiguous patterns such as `/Admin/*/Edit`, `/Admin/**`, and `/Admin*` are rejected.

`*` never invents permissions. A permission must already exist through Razor Page discovery or `DefinePermission(...)`.

## Partials, panels, buttons, and other capabilities

Not every permission needs to map to a full page. Register non-route capabilities explicitly:

```csharp
options.DefinePermission(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/ViewHistory");
```

Then use them for UI composition or feature gating:

```razor
@using AutoPolicy

@{
    var canEditRoles = await Model.HasAccessAsync(
        "/Administration/Accounts/Permissions/EditRoles");
}

@if (canEditRoles)
{
    <partial name="_RoleEditor" />
}
```

Or check multiple capabilities:

```csharp
var canManageAnything = await HttpContext.HasAnyAccessAsync(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/EditGroups");
```

Unknown in-page permission keys return `false`, even when the caller has a broad wildcard grant. In-page checks are for presentation and feature gating; they do not replace endpoint authorization for protected operations.

## Anonymous pages and opt-in protection

Use normal ASP.NET Core metadata when appropriate:

```csharp
[AllowAnonymous]
public class LoginModel : PageModel
{
}
```

or configure exact/prefix patterns:

```csharp
options.AllowAnonymous(
    "/Account/Login",
    "/Public/*");
```

Bare `AllowAnonymous("*")` is rejected.

If an application intentionally wants opt-in protection instead of the default-deny convention:

```csharp
options.ProtectRazorPagesByDefault(false);
```

Specific PageModels can then be protected with `[AutoPolicy]`.

## Permission overrides and aliases

Remap one discovered page to another canonical permission key:

```csharp
options.OverridePermissionKey(
    "/Administration/LegacyPage",
    "/Administration/Accounts/Manage");
```

Add a legacy or alternate key that resolves to a registered permission:

```csharp
options.AddAlias(
    "/Legacy/ManageAccounts",
    "/Administration/Accounts/Manage");
```

Conflicting mappings, cycles, aliases that shadow registered permissions, and aliases whose final target is not registered fail startup validation.

## Permission-denied responses

Keep the host application's existing ASP.NET Core behavior:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

Or return a direct `403 Forbidden` for AutoPolicy forbids:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

Authentication challenges and unrelated authorization policies remain controlled by the host application.

## Built-in claims adapter

When no custom `IAutoPolicyAccessProvider` is registered, AutoPolicy uses authenticated `ClaimsIdentity` instances.

| Access field | Default claim type |
| --- | --- |
| Allow roles | `ClaimTypes.Role` |
| Allow groups | `permission_group` |
| Allow permissions | `permission_allow` |
| Deny roles | `permission_deny_role` |
| Deny groups | `permission_deny_group` |
| Deny permissions | `permission_deny` |

Anonymous identities contribute no access.

Custom claim mappings can be registered before `AddAutoPolicy`:

```csharp
builder.Services.AddScoped<IAutoPolicyAccessProvider>(_ =>
    new ClaimsAutoPolicyAccessProvider(new ClaimsPermissionOptions
    {
        GroupClaimType = "app_group",
        AllowPermissionClaimType = "app_permission"
    }));

builder.Services.AddAutoPolicy();
```

## Registry and startup validation

`IPermissionRegistry` exposes the canonical permissions AutoPolicy knows about for diagnostics and administration tooling:

```csharp
var registry = services.GetRequiredService<IPermissionRegistry>();
var permissions = registry.GetAll();
```

Startup validation rejects structural problems such as duplicate canonical keys, cyclic groups, missing included groups, invalid aliases, and invalid page overrides.

Enable strict validation to also turn stale exact references and wildcard patterns that match nothing into startup errors:

```csharp
options.StrictValidation = true;
```

## Security behavior

AutoPolicy is deliberately fail-closed:

- protected pages with no grants are denied;
- provider exceptions and invalid provider results deny access;
- unknown permissions do not become grantable through wildcards;
- malformed permission patterns are rejected;
- configuration conflicts fail startup;
- deny rules override all allow sources;
- request cancellation is propagated rather than converted into an authorization result.

## Public package and quality gates

The package targets `.NET 10` and references the shared `Microsoft.AspNetCore.App` framework rather than splitting ASP.NET Core support into another AutoPolicy package.

The repository workflow continuously checks:

- Release-configuration tests;
- package creation;
- NuGet package metadata and contents;
- clean packaged-consumer restore;
- packaged-consumer smoke execution.

Run the same core checks locally:

```bash
dotnet test AutoPolicy.slnx --configuration Release
dotnet pack src/AutoPolicy/AutoPolicy.csproj --configuration Release --output artifacts
```

```powershell
./scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

## Repository

```text
src/AutoPolicy/                    AutoPolicy NuGet package source
tests/AutoPolicy.Tests/            Unit and security tests
tests/AutoPolicy.TestApp/          Razor Pages integration-test host
smoke/AutoPolicy.ConsumerSmoke/    Packaged-consumer smoke test
scripts/                           Package validation scripts
docs/                              Authorization design documentation
AutoPolicy.slnx                    Development solution
```

See [`docs/AUTHORIZATION.md`](docs/AUTHORIZATION.md) for the authorization contract.

## License

Mozilla Public License 2.0 (`MPL-2.0`).
