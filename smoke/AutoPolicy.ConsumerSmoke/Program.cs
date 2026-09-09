using AutoPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

var services = new ServiceCollection();
services.AddLogging();
services.AddAutoPolicy();

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

var access = await accessProvider.GetAccessAsync(new DefaultHttpContext());
Require(
    access.IsEmpty,
    "The default claims access provider should fail closed when no authenticated access exists.");
Require(
    AutoPolicyDefaults.PolicyName == "AutoPolicy",
    "The packaged consumer observed an unexpected default policy name.");

Console.WriteLine("Packaged AutoPolicy consumer smoke test passed.");
