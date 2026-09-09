# AutoPolicy

Automatic, default-deny route permissions for ASP.NET Core Razor Pages.

AutoPolicy maps Razor Pages to canonical permission identities and evaluates access through ASP.NET Core's standard authorization pipeline. It is designed to keep route authorization centralized instead of scattering role checks throughout page handlers.

## Basic setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddAutoPolicy(options =>
{
    options.AllowAnonymous("/Account/Login", "/Error");
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
```

`AddAutoPolicy` registers the authorization requirement and handler, permission registry and evaluator, Razor Pages application-model integration, and startup validation.

## Package status

The public API is still being refined for the first `0.1.0` release. Review the repository changelog when adopting a pre-release build.

## Project

Source, documentation, tests, and release history are maintained at the AutoPolicy GitHub repository.

## License

Mozilla Public License 2.0 (`MPL-2.0`).
