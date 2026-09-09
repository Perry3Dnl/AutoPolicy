# AutoPolicy Authorization Model

**Status:** development contract for the first `0.1.0` release

AutoPolicy is an authorization library. It discovers permission identities and evaluates access; it is deliberately not an identity, account-management, or permission-persistence system.

## 1. Minimal page usage

Most Razor Pages do nothing permission-specific.

A page such as:

```text
/Staff/Profile/ProfileDetails
```

is discovered automatically and receives a canonical AutoPolicy permission key derived from its Razor Page identity. Query-string values and route parameter values are not part of that permission identity.

Razor Pages are protected by default unless they are explicitly anonymous or the application disables the default protection convention.

## 2. Ownership boundary

### The host application owns

- authentication and user identity
- account storage and account lifecycle
- permission persistence
- role/group assignment to users
- direct user permission grants and denies
- session/database/cache integration
- admin screens that edit assignments

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
- request-level `HasAccessAsync(...)` checks
- permission registry diagnostics
- startup validation

AutoPolicy must never require an application to use a particular user table, role enum, JSON schema, session format, database, or authentication mechanism.

## 3. The integration contract

The application supplies one request-scoped access snapshot through:

```csharp
public interface IAutoPolicyAccessProvider
{
    ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
```

Example adapter:

```csharp
public sealed class TslAccessProvider : IAutoPolicyAccessProvider
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

The provider may use claims, Identity, session state, SQL, Redis, tenant information, an external API, or any other application-specific source. AutoPolicy does not distinguish between them.

The access snapshot is cached for the current HTTP request, so route authorization and multiple in-page checks reuse the same resolved state.

## 4. Roles and groups

AutoPolicy may define what a role or group *means*:

```csharp
options.DefineGroup("AccountPermissionEditors", group =>
{
    group.Include("/Administration/Accounts/AccountPermissions/EditRoles");
    group.Include("/Administration/Accounts/AccountPermissions/EditGroups");
});

options.DefineRole("Admin", role =>
{
    role.IncludeGroup("AccountPermissionEditors");
});
```

But AutoPolicy does not assign `Admin` or `AccountPermissionEditors` to a user. The host application returns those names in `AutoPolicyAccess.AllowRoles`, `AllowGroups`, `DenyRoles`, or `DenyGroups`.

This keeps the permission model code-defined while leaving account administration and persistence in the application.

## 5. Explicit non-route capabilities

Not every permission maps to a full Razor Page. Partials, panels, buttons, menu items, tabs, and operations can have their own capability identity.

Register them explicitly:

```csharp
options.DefinePermission(
    "/Administration/Accounts/AccountPermissions/EditRoles",
    "/Administration/Accounts/AccountPermissions/EditGroups",
    "/Administration/Accounts/AccountPermissions/ViewHistory");
```

These are concrete permission identities, not route declarations. `DefinePermission(...)` rejects wildcard identities; wildcard patterns belong in grants, groups, and roles.

Explicit capabilities enter the same permission registry as discovered pages. They therefore participate in the same:

- role/group configuration
- wildcard matching
- direct allow/deny rules
- alias resolution
- deny-wins evaluation
- startup diagnostics
- effective-permission enumeration

## 6. In-page checks

Use the same evaluator for conditional UI:

```csharp
var canEditRoles = await Model.HasAccessAsync(
    "/Administration/Accounts/AccountPermissions/EditRoles");
```

For menus or sections where any one of several capabilities is enough:

```csharp
var canManageAnything = await Model.HasAnyAccessAsync(
    "/Administration/Accounts/AccountPermissions/EditRoles",
    "/Administration/Accounts/AccountPermissions/EditGroups");
```

The same methods are available from `HttpContext`.

An unknown key returns `false` without loading the access provider. This intentionally prevents a typo from becoming grantable through a broad wildcard such as `/Administration/*`.

In-page checks are for UI composition and feature gating. They do not replace server-side authorization for an endpoint that performs a protected operation.

## 7. Deny-wins evaluation

The effective rule is conceptually:

```text
(allow roles + allow groups + direct allows)
-
(deny roles + deny groups + direct denies)
```

After roles and groups expand to permission patterns, any matching deny takes precedence over all matching allows.

The application may store assignments however it wants. AutoPolicy evaluates only the `AutoPolicyAccess` snapshot it receives.

## 8. Fail-closed behavior

AutoPolicy is designed so that missing or invalid authorization state never creates accidental access.

- no access source produces no grants
- a provider returning `null` is invalid and authorization fails
- provider failures deny protected requests
- permission-evaluation failures deny protected requests
- unresolved page mappings deny access
- unknown in-page permissions return `false`
- alias cycles fail validation/resolution
- direct denies override grants

Explicitly anonymous routes are the exception because the application intentionally marked them public.

## 9. Permission-denied responses

The default response behavior remains the application's normal ASP.NET Core authorization behavior:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.Default;
```

For applications that want AutoPolicy forbids to render as direct HTTP 403 responses instead of an authentication handler's access-denied redirect:

```csharp
options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
```

Authentication challenges still use the application's normal behavior. Unrelated authorization policies are delegated to the application's existing middleware result handler.

## 10. Non-goals

AutoPolicy does not provide APIs such as:

```csharp
AutoPolicy.CreateUser(...);
AutoPolicy.AssignRole(userId, "Admin");
AutoPolicy.SavePermissions(...);
```

Those APIs would cross the library boundary and couple AutoPolicy to application data models.

The intended contract is:

> **AutoPolicy defines and evaluates access. The application owns identities, persistence, and assignments.**
