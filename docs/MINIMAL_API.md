# Minimal API authorization

AutoPolicy protects ASP.NET Core endpoints by default through the authorization system's fallback policy. Every Minimal API endpoint that reaches `UseAuthorization()` requires a registered permission unless it is marked `AllowAnonymous`.

## Default protection

Configure AutoPolicy as usual:

```csharp
builder.Services.AddAutoPolicy();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/analytics", GetAnalytics);
app.MapPost("/packages/upload", UploadPackage);
```

No per-endpoint AutoPolicy call is needed. Unless an endpoint is explicitly anonymous, its route pattern becomes its permission key (`/analytics`, `/packages/upload`). The key is discovered when the endpoint is authorized and registered with the shared permission registry. With no matching grant, AutoPolicy denies the request.

ASP.NET Core authorization policies remain active. AutoPolicy adds its permission requirement to fallback, default, and named authorization policies, so adding another `[Authorize]` policy does not bypass the permission check.

## Public endpoints

Use standard ASP.NET Core `AllowAnonymous` metadata to exclude an endpoint from authorization:

```csharp
app.MapGet("/health", GetHealth).AllowAnonymous();
```

Razor Pages continue to use `RazorPagesProtectedByDefault`; disabling that option does not disable the Minimal API fallback policy.

## Explicit permission keys

Use `RequireAutoPolicy` when a permission should have a stable capability name independent of the route:

```csharp
app.MapGet("/analytics", GetAnalytics)
    .RequireAutoPolicy("Dashboard.Analytics.View");

app.MapPost("/packages/upload", UploadPackage)
    .RequireAutoPolicy("Dashboard.Packages.Upload");
```

Explicit endpoint permissions are discovered and registered at startup. They use the existing access provider, permission registry, aliases, roles, groups, wildcard rules, and deny-wins evaluator.

## Opt out of default endpoint protection

If an application needs an opt-in model for Minimal APIs, it can turn off the fallback protection. Individual endpoints can still require AutoPolicy explicitly:

```csharp
builder.Services.AddAutoPolicy(options =>
{
    options.ProtectMinimalApisByDefault(false);
});

app.MapGet("/analytics", GetAnalytics)
    .RequireAutoPolicy("Dashboard.Analytics.View");
```