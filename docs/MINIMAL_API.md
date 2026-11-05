# ASP.NET Core endpoint authorization

AutoPolicy protects ASP.NET Core endpoint-routed requests by default through the authorization system's fallback policy. This covers MVC controllers, Razor Pages, Minimal APIs, gRPC RPC endpoints, mapped static asset endpoints, and SignalR connection endpoints that reach `UseAuthorization()`. A request without a matching permission is denied unless it is marked `AllowAnonymous`.

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

Razor Pages also use `RazorPagesProtectedByDefault`; that option is independent of endpoint protection.

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
    options.ProtectEndpointsByDefault(false);
});

app.MapGet("/analytics", GetAnalytics)
    .RequireAutoPolicy("Dashboard.Analytics.View");
```
## gRPC services

gRPC services mapped with `MapGrpcService` use ASP.NET Core endpoint routing, so each RPC is protected by the fallback policy without adding per-service authorization attributes. The RPC route becomes the permission key (for example, `/test.Probe/Get` for a service named `test.Probe` and a method named `Get`).

A streaming RPC is authorized when its call starts. If permission must be rechecked for each message in a long-lived stream, add application-level checks in the service or a gRPC interceptor.

## Static assets

Static files mapped with `MapStaticAssets()` are endpoint-routed and receive default AutoPolicy protection. Grant their route permissions or mark the static asset endpoints anonymous when the assets are public:

```csharp
app.MapStaticAssets().Add(endpointBuilder =>
    endpointBuilder.Metadata.Add(new AllowAnonymousAttribute()));
```

That convention makes all mapped static assets public. The traditional `UseStaticFiles()` middleware is commonly placed before `UseAuthorization()` and then bypasses endpoint authorization entirely. Treat that as an explicit public-assets choice.
## MVC and endpoint-routed frameworks

MVC controller actions and Razor Pages are protected when they are mapped into endpoint routing and the request passes through `UseAuthorization()`. Route patterns are the permission keys for these endpoints. Existing `[Authorize]` policies continue to apply, and AutoPolicy adds its permission check to them. Standard `[AllowAnonymous]` metadata remains public.

## SignalR hubs

AutoPolicy protects the hub connection endpoint and each hub method. The connection uses its mapped route permission. Hub methods use a key based on the hub and method name, for example `/SignalR/Chat/SendMessage`. A custom claims provider can grant these keys like other permission keys.

## Interactive server-side Blazor

Interactive component navigation runs through Blazor's router, so it does not pass through endpoint authorization for every navigation. Protect all routed components with the standard authorization view and a global component attribute:

```razor
@* _Imports.razor *@
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]
```

```razor
<AuthorizeRouteView RouteData="routeData"
                    Resource="routeData"
                    DefaultLayout="typeof(MainLayout)" />
```

AutoPolicy derives the permission from the component's route template. Mark intentionally public components with `[AllowAnonymous]`. The built-in claims provider supports principal-only component checks. A custom provider must also implement `IAutoPolicyPrincipalAccessProvider`; otherwise component access fails closed.

Blazor WebAssembly cannot use server-side fallback authorization. Enforce permissions on the server endpoints that return data or perform operations. Static files served before authorization middleware are outside endpoint fallback protection.