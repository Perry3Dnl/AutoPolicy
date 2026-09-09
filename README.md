# AutoPolicy

Automatic, default-deny route permissions for ASP.NET Core Razor Pages.

AutoPolicy discovers Razor Pages, assigns stable permission identities, and evaluates access through ASP.NET Core authorization. Normal pages require no AutoPolicy attributes, policy strings, custom base classes, or injected permission services.

## Install

```bash
dotnet add package AutoPolicy --version 0.1.0
```

AutoPolicy `0.1.0` targets .NET 10.

## Quick start

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

Razor Pages are protected by default. With no access source, protected pages deny access.

## Automatic page permissions

Permission identity comes from the Razor Page itself, not the browser URL at runtime.

```text
Pages/Staff/Members/Detail.cshtml
→ /Staff/Members/Detail

Areas/BackOffice/Pages/Dashboard.cshtml
→ /BackOffice/Dashboard
```

Custom `@page` route templates, route values, query strings, and fragments do not change the permission identity. Permission keys are normalized and compared case-insensitively.

For example, this page still requires `/Members/Detail`:

```razor
@page "/members/{id:int}"
```

whether the request is `/members/12`, `/members/900`, or `/members/12?tab=history`.

## AutoPolicy does not manage users

AutoPolicy defines and evaluates access. Your application owns:

- authentication and user accounts
- databases, sessions, claims, Redis, APIs, or other permission sources
- assigning roles and groups
- direct per-user grants and denies
- administration screens that edit assignments

`IAutoPolicyAccessProvider` is the integration boundary.

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

Register a custom provider before `AddAutoPolicy`:

```csharp
builder.Services.AddScoped<IAutoPolicyAccessProvider, ApplicationAccessProvider>();
builder.Services.AddAutoPolicy(options =>
{
    // permission definitions
});
```

The returned `AutoPolicyAccess` snapshot is cached once per HTTP request, so route authorization and repeated in-page checks use the same resolved state.

## Roles, groups, and direct permissions

Roles and groups are application-defined names. AutoPolicy defines what they mean; your application decides which names apply to the current request.

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

The provider can then return:

```csharp
new AutoPolicyAccess
{
    AllowRoles = ["Admin"],
    DenyPermissions = ["/Reports/Payroll"]
};
```

A deny always wins, regardless of how many roles, groups, or direct grants also allow the permission.

## Wildcards

AutoPolicy intentionally supports a small wildcard grammar:

```text
/Admin/Users       exact permission
/Admin/*           descendants of /Admin
*                  every registered permission
```

`/Admin/*` matches `/Admin/Users` and `/Admin/Users/Edit`, but not `/Admin`, `/Administrator`, or `/Admin2/Users`.

Embedded or ambiguous wildcards such as `/Admin/*/Edit`, `/Admin/**`, and `/Admin*` are rejected.

`*` never invents permissions. A permission must first exist in the AutoPolicy registry through Razor Page discovery or `DefinePermission(...)`.

## Partials, panels, buttons, and other capabilities

Not every permission needs to map to a full page. Register non-route capabilities explicitly:

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

Unknown in-page permission keys return `false`, even when the caller has a broad wildcard grant. This prevents a typo from becoming implicitly authorized.

In-page checks are for UI composition and feature gating. They do not replace server-side authorization on an endpoint that performs a protected operation.

## Anonymous pages and opt-in protection

Use standard ASP.NET Core metadata:

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

Bare `AllowAnonymous("*")` is rejected. If you intentionally want an opt-in model instead of the default-deny convention:

```csharp
options.ProtectRazorPagesByDefault(false);
```

Then mark specific PageModels with `[AutoPolicy]` to protect them.

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

The default keeps the application's existing ASP.NET Core behavior:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

For a direct `403 Forbidden` on AutoPolicy forbids:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

Authentication challenges and unrelated authorization policies remain controlled by the host application.

## Built-in claims adapter

If you do not register a custom `IAutoPolicyAccessProvider`, AutoPolicy uses authenticated `ClaimsIdentity` instances.

| Access field | Default claim type |
| --- | --- |
| Allow roles | `ClaimTypes.Role` |
| Allow groups | `permission_group` |
| Allow permissions | `permission_allow` |
| Deny roles | `permission_deny_role` |
| Deny groups | `permission_deny_group` |
| Deny permissions | `permission_deny` |

Anonymous identities contribute no access.

For custom claim-type mappings, register `ClaimsAutoPolicyAccessProvider` yourself before `AddAutoPolicy`:

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

Startup validation rejects structural problems such as duplicate canonical keys, cyclic groups, missing included groups, invalid aliases, and invalid page overrides. Set `StrictValidation = true` to also treat stale exact permission references and wildcard patterns that match nothing as startup errors instead of warnings.

## Security behavior

AutoPolicy is deliberately fail-closed:

- protected pages with no grants are denied
- provider exceptions and invalid provider results deny access
- unknown permissions do not become grantable through wildcards
- malformed permission patterns are rejected
- configuration conflicts fail startup
- deny rules override all allow sources
- request cancellation is propagated rather than converted into an authorization result

## Repository

```text
src/AutoPolicy/                    NuGet package source
tests/AutoPolicy.Tests/            Unit and security tests
tests/AutoPolicy.TestApp/          Razor Pages integration-test host
smoke/AutoPolicy.ConsumerSmoke/    Packaged-consumer smoke test
scripts/                           NuGet validation scripts
docs/                              Authorization design documentation
AutoPolicy.slnx                    Development solution
```

Development commands:

```bash
dotnet test AutoPolicy.slnx --configuration Release
dotnet pack src/AutoPolicy/AutoPolicy.csproj --configuration Release --output artifacts
```

```powershell
./scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

See [`docs/AUTHORIZATION.md`](docs/AUTHORIZATION.md) for the authorization contract.

## License

Mozilla Public License 2.0 (`MPL-2.0`).
