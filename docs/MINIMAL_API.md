# Minimal API authorization

AutoPolicy can protect Minimal API endpoints through ASP.NET Core endpoint authorization metadata. The same access provider, permission registry, aliases, roles, groups, wildcard rules, and deny-wins evaluator used by Razor Pages handle these requests.

## Require a named permission

```csharp
app.MapGet("/analytics", GetAnalytics)
    .RequireAutoPolicy("Dashboard.Analytics.View");

app.MapPost("/packages/upload", UploadPackage)
    .RequireAutoPolicy("Dashboard.Packages.Upload");
```

An explicit key is useful when the permission represents an application capability rather than a route. AutoPolicy discovers and registers the key at startup, so it does not need to be repeated in `DefinePermission(...)`.

## Derive the permission from the route

When the route itself is the permission identity, omit the key:

```csharp
app.MapGet("/analytics", GetAnalytics)
    .RequireAutoPolicy();
```

This endpoint uses `/analytics` as its registered permission key. Route parameters remain part of the route pattern, so `/packages/{packageId}` maps to `/packages/{packageId}`.

A route group can apply the requirement to all endpoints in that group:

```csharp
var dashboard = app.MapGroup("/dashboard")
    .RequireAutoPolicy();

dashboard.MapGet("/analytics", GetAnalytics);
dashboard.MapPost("/packages/upload", UploadPackage);
```

For a route group, the route-derived permission includes the group's prefix and the endpoint route.

## Authorization behavior

`RequireAutoPolicy(...)` adds AutoPolicy's ASP.NET Core authorization policy to the endpoint. `UseAuthorization()` must be present in the request pipeline. Requests without a matching grant are denied; explicit deny rules still take precedence over allows.

Endpoints without `RequireAutoPolicy(...)` are not automatically protected by this feature. Razor Page default protection remains controlled separately by `RazorPagesProtectedByDefault`.

Standard ASP.NET Core endpoint authorization continues to work alongside AutoPolicy:

```csharp
app.MapGet("/health", GetHealth).AllowAnonymous();
```

Configure an access provider once through `AddAutoPolicy(...)`; endpoint authorization uses that provider and the existing claims fallback.