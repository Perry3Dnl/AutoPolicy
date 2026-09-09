# Changelog

All notable changes to AutoPolicy will be documented in this file.

## [Unreleased]

- Initial ASP.NET Core authorization implementation is under active development.
- Added repository build, test, package validation, smoke-test, and release infrastructure.
- Centralized package version and NuGet metadata in `Directory.Build.props`.
- Replaced the user-bound access model with request-scoped `AutoPolicyAccess` and `IAutoPolicyAccessProvider` abstractions.
- Added a built-in claims adapter while keeping custom non-claims access providers supported.
- Removed the authentication check from the core authorization handler so custom providers can authorize independently of `ClaimsPrincipal` authentication state.

`0.1.0` has not been released yet.
