using AutoPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

const string manageCapability = "/Ui/AccountPermissions/EditRoles";

var services = new ServiceCollection();
services.AddLogging();
services.AddAutoPolicy(options =>
{
    options.DefinePermission(manageCapability);
});

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

Require(
    scope.ServiceProvider.GetService<IPermissionRegistry>() is not null,
    "The packaged consumer could not resolve IPermissionRegistry.");
Require(
    scope.ServiceProvider.GetService<IPermissionEvaluator>() is not null,
    "The packaged consumer could not resolve IPermissionEvaluator.");

var accessProvider = scope.ServiceProvider.GetService<IAutoPolicyAccessProvider>();
Require(
    accessProvider is ClaimsAutoPolicyAccessProvider,
    "The packaged consumer could not resolve the built-in claims access provider.");

var anonymousAccess = await accessProvider.GetAccessAsync(new DefaultHttpContext());
Require(
    anonymousAccess.IsEmpty,
    "The default claims access provider should fail closed when no authenticated access exists.");

var context = new DefaultHttpContext
{
    RequestServices = scope.ServiceProvider,
    User = new ClaimsPrincipal(new ClaimsIdentity(
    [
        new Claim(ClaimsAutoPolicyAccessProvider.DefaultAllowPermissionClaimType, manageCapability)
    ], "smoke"))
};

Require(
    await context.HasAccessAsync(manageCapability),
    "The packaged consumer could not authorize an explicitly registered in-page capability.");
Require(
    !await context.HasAccessAsync("/Ui/UnknownCapability"),
    "Unknown in-page capabilities must fail closed.");

var effectivePermissions = await context.GetEffectivePermissionsAsync();
Require(
    effectivePermissions.Contains(manageCapability, StringComparer.OrdinalIgnoreCase),
    "The explicit capability was not included in effective permission enumeration.");

Require(
    AutoPolicyDefaults.PolicyName == "AutoPolicy",
    "The packaged consumer observed an unexpected default policy name.");

Console.WriteLine("Packaged AutoPolicy consumer smoke test passed.");
