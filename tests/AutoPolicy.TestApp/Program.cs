using System.Security.Claims;
using System.Text.Encodings.Web;
using AutoPolicy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(TestAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
        TestAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddSingleton<IAutoPolicyAccessProvider, HeaderAutoPolicyAccessProvider>();
builder.Services.AddAutoPolicy(options =>
{
    options.PermissionDeniedBehavior = PermissionDeniedBehavior.StatusCode403;
    options.AllowAnonymous("/Public/*");
    options.OverridePermissionKey("/Remapped", "/Virtual/Remapped");

    options.DefineGroup("NestedPages", group =>
        group.Include("/Nested/*"));

    options.DefineGroup("ProbeAccess", group =>
        group.Include("/Probe"));

    options.DefineGroup("StaffBase", group =>
    {
        group.IncludeGroup("NestedPages");
        group.IncludeGroup("ProbeAccess");
    });

    options.DefineRole("Staff", role =>
        role.IncludeGroup("StaffBase"));

    options.DefineRole("Administrator", role =>
        role.Include(PermissionPattern.MatchAll));
});

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AutoPolicy.Tests";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "integration-test")],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class HeaderAutoPolicyAccessProvider : IAutoPolicyAccessProvider
{
    public ValueTask<AutoPolicyAccess> GetAccessAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        return ValueTask.FromResult(new AutoPolicyAccess
        {
            AllowRoles = Read(context, "X-AutoPolicy-Allow-Role"),
            AllowGroups = Read(context, "X-AutoPolicy-Allow-Group"),
            AllowPermissions = Read(context, "X-AutoPolicy-Allow"),
            DenyRoles = Read(context, "X-AutoPolicy-Deny-Role"),
            DenyGroups = Read(context, "X-AutoPolicy-Deny-Group"),
            DenyPermissions = Read(context, "X-AutoPolicy-Deny")
        });
    }

    private static IReadOnlyCollection<string> Read(HttpContext context, string headerName)
    {
        return context.Request.Headers[headerName]
            .SelectMany(value => value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [])
            .ToArray();
    }
}
