# AutoPolicy

Automatic, default-deny route permissions for ASP.NET Core Razor Pages.

> **Development status:** AutoPolicy is under active development toward `0.1.0`. No `0.1.0` release has been published yet.

AutoPolicy discovers Razor Pages, maps them to canonical permission identities, and evaluates access through the standard ASP.NET Core authorization pipeline. It supports roles, groups, direct grants and denies, explicit non-route capabilities, aliases, anonymous route patterns, and deny-wins evaluation.

## Scope and ownership

AutoPolicy **defines and evaluates access**. It does not manage users or permission storage.

The host application owns:

- user accounts and authentication
- databases, sessions, claims, Redis, APIs, or any other permission source
- assigning roles and groups to users
- direct per-user permission grants and denies
- administration screens used to edit those assignments

AutoPolicy owns:

- Razor Page discovery and canonical permission keys
- explicit application capability registration
- role and group definitions
- allow/deny expansion with deny-wins semantics
- route authorization
- in-page `HasAccessAsync(...)` checks
- registry diagnostics and startup validation

`IAutoPolicyAccessProvider` is the integration boundary between the two.

## Basic setup

```csharp
builder.Services.AddRazorPages();

builder.Services.AddAutoPolicy(options =>
{
    options.AllowAnonymous("/Account/Login", "/Error");

    options.DefinePermission(
        "/Administration/Accounts/AccountPermissions/EditRoles");

    options.DefineGroup("AccountPermissionEditors", group =>
    {
        group.Include("/Administration/Accounts/AccountPermissions/EditRoles");
    });

    options.DefineRole("Admin", role =>
    {
        role.IncludeGroup("AccountPermissionEditors");
    });
});
```

Normal Razor Pages do not need permission attributes or page-specific policy strings. Their permission identity is discovered automatically.

## Integrating application permissions

Applications with their own permission persistence implement `IAutoPolicyAccessProvider`:

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

Register it with normal ASP.NET Core dependency injection:

```csharp
builder.Services.AddScoped<IAutoPolicyAccessProvider, ApplicationAccessProvider>();
builder.Services.AddAutoPolicy(...);
```

AutoPolicy does not know how those assignments were stored or edited. The built-in claims provider is only a convenient default adapter.

## Partials and in-page capabilities

A partial, button, menu item, panel, or operation can require a permission different from its containing page. Register that capability explicitly:

```csharp
options.DefinePermission(
    "/Administration/Accounts/AccountPermissions/EditRoles",
    "/Administration/Accounts/AccountPermissions/ViewHistory");
```

Then check it from a Razor Page:

```csharp
var canEditRoles = await Model.HasAccessAsync(
    "/Administration/Accounts/AccountPermissions/EditRoles");
```

or from `HttpContext`:

```csharp
var canSeeAnything = await HttpContext.HasAnyAccessAsync(
    "/Administration/Accounts/AccountPermissions/EditRoles",
    "/Administration/Accounts/AccountPermissions/ViewHistory");
```

These checks use the same provider, role/group expansion, aliases, wildcard rules, and deny-wins evaluator as route authorization. The access snapshot is loaded at most once per HTTP request. Unknown in-page permission keys fail closed; non-route capabilities must be registered with `DefinePermission(...)`.

## Permission-denied response behavior

By default AutoPolicy delegates forbidden responses to the application's existing ASP.NET Core authorization behavior:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

To return a direct `403 Forbidden` for AutoPolicy forbids while preserving normal authentication challenges:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

Unrelated authorization policies continue through the application's existing authorization result handler.

## Repository layout

```text
src/AutoPolicy/                    Package source
tests/AutoPolicy.Tests/            Automated tests
smoke/AutoPolicy.ConsumerSmoke/    Packaged-consumer smoke test
scripts/                           Package validation scripts
docs/                              Design and integration documentation
AutoPolicy.slnx                    Main development solution
```

See [`docs/AUTHORIZATION.md`](docs/AUTHORIZATION.md) for the authorization model and integration contract.

## Development

Open `AutoPolicy.slnx` in Visual Studio or another .NET-compatible IDE.

```bash
dotnet test AutoPolicy.slnx --configuration Release
dotnet pack src/AutoPolicy/AutoPolicy.csproj --configuration Release --output artifacts
```

Validate the generated package:

```powershell
./scripts/Validate-Packages.ps1 -ArtifactsPath ./artifacts
```

The version under development is defined centrally in `Directory.Build.props`.

## Releases

Publishing is intentionally tag-gated. Normal pushes and pull requests build, test, pack, and validate the project but do not publish it. A NuGet release can only run from a matching `v*` tag through the protected GitHub `release` environment.

## License

AutoPolicy is licensed under the Mozilla Public License 2.0 (`MPL-2.0`).
