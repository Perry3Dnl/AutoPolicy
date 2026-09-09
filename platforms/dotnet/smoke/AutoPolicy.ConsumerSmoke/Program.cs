using AutoPolicy;
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
Require(
    scope.ServiceProvider.GetService<IUserPermissionProvider>() is not null,
    "The packaged consumer could not resolve IUserPermissionProvider.");
Require(
    AutoPolicyDefaults.PolicyName == "AutoPolicy",
    "The packaged consumer observed an unexpected default policy name.");

Console.WriteLine("Packaged AutoPolicy consumer smoke test passed.");
