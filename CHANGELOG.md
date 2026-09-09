# Changelog

All notable changes to AutoPolicy will be documented in this file.

## [Unreleased]

- Initial ASP.NET Core authorization implementation is under active development.
- Added repository build, test, package validation, smoke-test, and release infrastructure.
- Centralized package version and NuGet metadata in `Directory.Build.props`.
- Replaced the user-bound access model with request-scoped `AutoPolicyAccess` and `IAutoPolicyAccessProvider` abstractions.
- Added a built-in claims adapter while keeping custom non-claims access providers supported.
- Removed the authentication check from the core authorization handler so custom providers can authorize independently of `ClaimsPrincipal` authentication state.
- Flattened the repository to a conventional .NET layout with root `src`, `tests`, `smoke`, and `scripts` folders plus `AutoPolicy.slnx`.
- Added explicitly registered non-route permissions for partials, page sections, buttons, menus, and other application capabilities.
- Added `HasAccessAsync`, `HasAnyAccessAsync`, and effective-permission helpers for Razor Page models and `HttpContext`.
- Cached the application access snapshot once per HTTP request so route and in-page authorization share the same resolved state.
- Made unknown in-page permissions fail closed even when a broad wildcard grant would otherwise match.
- Centralized alias resolution and added alias-cycle validation.
- Documented the application integration boundary: AutoPolicy defines and evaluates access while the host owns identities, persistence, and assignments.
- Added configurable permission-denied behavior with the ASP.NET Core default as the default and an opt-in direct HTTP 403 mode.
- Hardened wildcard parsing, registry-bound authorization, anonymous-pattern safety, and deny-precedence coverage.
- Added real Razor Pages integration coverage for automatic discovery, custom routes, Areas, overrides, anonymous pages, and handler gating.
- Hardened startup validation so conflicting aliases/overrides, invalid alias targets, unknown group references, missing override sources, and registration collisions fail deterministically.
- Added failure-path coverage for provider exceptions, malformed access snapshots, null provider results, and request cancellation.

`0.1.0` has not been released yet.
