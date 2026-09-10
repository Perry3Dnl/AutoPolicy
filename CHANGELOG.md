# Changelog

All notable changes to AutoPolicy are documented in this file.

## [Unreleased]

## [0.1.1] - 2026-09-10

### Changed

- Added the official AutoPolicy shield artwork to the repository and package documentation.
- Added the AutoPolicy icon to NuGet package metadata so NuGet.org displays the correct package logo.
- Added package validation for the embedded icon file and NuGet `icon` metadata.
- Refreshed the GitHub and NuGet README branding to match AutoPolicy's current presentation.

## [0.1.0] - 2026-09-09

### Added

- Automatic ASP.NET Core Razor Page discovery with canonical, route-value-independent permission identities.
- Default-on Razor Page protection with standard `[AllowAnonymous]` support and validated anonymous permission patterns.
- `IAutoPolicyAccessProvider` and request-scoped `AutoPolicyAccess` as the application integration boundary.
- Built-in authenticated-claims adapter plus support for custom database, session, cache, tenant, or API-backed providers.
- Application-defined roles and nested permission groups.
- Direct allow and deny roles, groups, and permission patterns with deny-wins evaluation.
- Exact permission rules, trailing `/*` prefix wildcards, and the `*` match-all rule for registered permissions.
- Explicit non-route permissions for partials, panels, buttons, menus, tabs, and other application capabilities.
- `HasAccessAsync`, `HasAnyAccessAsync`, and effective-permission helpers for Razor Page models and `HttpContext`.
- Request-level caching so route authorization and in-page checks share one resolved access snapshot.
- Permission-key overrides and alias chains with startup validation.
- `IPermissionRegistry` diagnostics for discovered and explicitly registered permissions.
- Configurable permission-denied behavior: ASP.NET Core default or direct HTTP 403 for AutoPolicy forbids.
- Package build, symbol package, metadata validation, packaged-consumer smoke tests, and tag-gated NuGet Trusted Publishing workflow.
- GitHub and NuGet documentation covering setup, integration, wildcards, in-page capabilities, claims, diagnostics, and security behavior.

### Security

- Protected pages fail closed when no access is supplied.
- Unknown permissions cannot become authorized through broad wildcard grants.
- Malformed and ambiguous wildcard patterns are rejected.
- Bare `AllowAnonymous("*")` is rejected to prevent accidental global exposure.
- Duplicate canonical page mappings and explicit/page permission collisions fail startup.
- Duplicate role or group definitions are rejected immediately instead of silently replacing earlier definitions.
- Missing nested groups, cyclic groups, invalid aliases, alias shadowing, stale alias targets, and invalid page overrides fail startup as appropriate.
- Provider exceptions, null provider results, malformed provider permission patterns, unresolved mappings, and evaluator failures deny protected access.
- Request cancellation propagates instead of being converted into an authorization result.
- Denies override role, group, direct, wildcard, and match-all grants.

### Packaging

- Targets .NET 10 (`net10.0`).
- Package ID: `AutoPolicy`.
- License: MPL-2.0.
- Includes XML IntelliSense documentation and portable symbol package.
- Public API surface is intentionally limited to configuration, access-provider integration, diagnostics, and in-page access helpers.
