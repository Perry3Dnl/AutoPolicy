# AutoPolicy Authorization Model

**Status:** `0.1.0` authorization contract

AutoPolicy is an authorization library for ASP.NET Core Razor Pages. It discovers permission identities and evaluates access; it deliberately does not become an identity, account-management, or permission-persistence system.

## 1. Minimal page usage

Most Razor Pages contain no AutoPolicy-specific code.

```csharp
public sealed class ProfileDetailsModel : PageModel
{
}
```

A page such as:

```text
Pages/Staff/Profile/ProfileDetails.cshtml
```

is discovered automatically as:

```text
/Staff/Profile/ProfileDetails
```

Razor Pages are protected by default. Query strings, runtime route values, fragments, and custom `@page` route templates do not become part of the permission identity.

Areas are included in the canonical identity:

```text
Areas/BackOffice/Pages/Dashboard.cshtml
→ /BackOffice/Dashboard
```

## 2. Ownership boundary

### The host application owns

- authentication and user identity
- account storage and account lifecycle
- permission persistence
- assigning roles and groups
- direct user grants and denies
- session, database, cache, tenant, or API integration
- administration screens that edit assignments

### AutoPolicy owns

- canonical permission identities
- automatic Razor Page discovery
- explicit non-route permission registration
- role and group definitions
- role/group expansion
- wildcard permission patterns
- direct allow/deny evaluation
- deny-wins behavior
- authorization integration
- request-level in-page access checks
- permission registry diagnostics
- startup validation

The boundary is:

> **AutoPolicy defines and evaluates access. The application owns identities, persistence, and assignments.**

## 3. Application integration

The host supplies one access snapshot per request through:

```csharp
public interface IAutoPolicyAccessProvider
{
    ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
```

Example:

```csharp
public sealed class ApplicationAccessProvider : IAutoPolicyAccessProvider
{
    public async ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var stored = await LoadApplicationPermissions(context, cancellationToken);
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
            AllowPermissions = stored.AllowPermissionKeys,
            DenyPermissions = stored.DenyPermissionKeys
        };
    }
}
```

The provider may use claims, Identity, session state, SQL, Redis, tenant information, an external API, or any application-specific source.

The access snapshot is cached for the HTTP request so endpoint authorization and multiple in-page checks use the same resolved state.

If no custom provider is registered, AutoPolicy uses authenticated claims as a convenience adapter. Anonymous identities contribute no access.

## 4. Roles and groups

AutoPolicy defines what application role and group names mean:

```csharp
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
```

AutoPolicy does not assign `Admin` or `AccountPermissionEditors` to a user. The host returns those names in `AutoPolicyAccess.AllowRoles`, `AllowGroups`, `DenyRoles`, or `DenyGroups`.

Groups may include other groups. Cycles and missing group references are startup errors.

## 5. Permission patterns

The supported pattern grammar is intentionally small:

```text
/Administration/Users    exact key
/Administration/*        descendants of /Administration
*                        every registered permission
```

Only one trailing `/*` wildcard is supported. Embedded or ambiguous wildcard forms are rejected.

Prefix matching is segment-boundary aware. `/Admin/*` does not match `/Administrator`, `/Admin2`, or `/Admin` itself.

The match-all rule applies only to permissions in the registry. It cannot authorize an unknown or misspelled capability.

## 6. Explicit non-route capabilities

Partials, page sections, buttons, menu items, tabs, and operations can have their own permission identities:

```csharp
options.DefinePermission(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/EditGroups",
    "/Administration/Accounts/Permissions/ViewHistory");
```

These are concrete permission identities, not route declarations. Wildcards are invalid in `DefinePermission(...)`; wildcard patterns belong in grants, groups, and roles.

Explicit capabilities use the same registry, role/group expansion, wildcard matching, aliases, deny-wins evaluation, and effective-permission enumeration as discovered pages.

## 7. In-page checks

Conditional UI uses the same authorization model:

```csharp
var canEditRoles = await Model.HasAccessAsync(
    "/Administration/Accounts/Permissions/EditRoles");
```

or:

```csharp
var canManageAnything = await Model.HasAnyAccessAsync(
    "/Administration/Accounts/Permissions/EditRoles",
    "/Administration/Accounts/Permissions/EditGroups");
```

The same helpers are available from `HttpContext`.

An unknown key returns `false` before the provider is loaded. This prevents a typo from becoming authorized by a broad wildcard.

In-page checks are intended for UI composition and feature gating. They do not replace server-side authorization for an endpoint that performs a protected operation.

## 8. Allow and deny evaluation

Conceptually:

```text
(allow roles + allow groups + direct allows)
-
(deny roles + deny groups + direct denies)
```

Roles and groups first expand to permission patterns. If any deny pattern matches the required permission, access is denied even when one or more allow sources also match.

A single deny cannot be outvoted by multiple allows.

## 9. Anonymous and opt-in pages

Standard ASP.NET Core `[AllowAnonymous]` is respected.

Applications may also configure exact or prefix anonymous patterns:

```csharp
options.AllowAnonymous("/Account/Login", "/Public/*");
```

Bare `AllowAnonymous("*")` is rejected. An intentional global opt-out is expressed explicitly:

```csharp
options.ProtectRazorPagesByDefault(false);
```

When default protection is disabled, `[AutoPolicy]` opts an individual PageModel back into automatic permission protection.

## 10. Overrides and aliases

`OverridePermissionKey(...)` remaps one discovered canonical Razor Page identity to a different concrete permission key. The source must correspond to a discovered page.

`AddAlias(...)` creates an alternate or legacy key that resolves to a registered canonical permission. Alias chains are allowed; cycles, registered-key shadowing, and unresolved final targets are startup errors.

## 11. Fail-closed behavior

Missing or invalid authorization state must never create accidental access.

- no grants means no protected access
- provider exceptions deny protected requests
- a provider returning `null` is invalid and denies access
- malformed provider permission patterns deny evaluation
- unresolved endpoint mappings deny access
- unknown in-page permissions return `false`
- duplicate permission identities fail startup
- invalid aliases and overrides fail startup
- cyclic or missing group references fail startup
- direct and expanded denies override all grants
- request cancellation propagates rather than being converted into a denial

Explicitly anonymous routes are the intentional exception because the application marked them public.

## 12. Permission-denied responses

The default delegates the final forbid response to the application's ASP.NET Core authorization configuration:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

Applications may instead return a direct HTTP 403 for AutoPolicy forbids:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

Authentication challenges and unrelated authorization policies remain under host control.

## 13. Diagnostics and validation

`IPermissionRegistry` exposes discovered and explicitly registered canonical permissions for diagnostics and administration tooling.

Startup validation checks structural correctness. `StrictValidation = true` additionally treats stale exact permission references and wildcard patterns that match no registered permission as errors instead of warnings.

## 14. Non-goals

AutoPolicy intentionally does not provide account-management APIs such as:

```csharp
AutoPolicy.CreateUser(...);
AutoPolicy.AssignRole(userId, "Admin");
AutoPolicy.SavePermissions(...);
```

Those concerns remain in the host application.
