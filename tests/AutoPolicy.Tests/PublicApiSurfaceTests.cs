using Xunit;

namespace AutoPolicy.Tests;

public sealed class PublicApiSurfaceTests
{
    [Fact]
    public void ExportedTypes_AreLimitedToSupportedPublicApi()
    {
        var expected = new[]
        {
            "AutoPolicy.AutoPolicyAccess",
            "AutoPolicy.AutoPolicyAttribute",
            "AutoPolicy.AutoPolicyOptions",
            "AutoPolicy.AutoPolicyServiceCollectionExtensions",
            "AutoPolicy.ClaimsAutoPolicyAccessProvider",
            "AutoPolicy.ClaimsPermissionOptions",
            "AutoPolicy.HttpContextPermissionExtensions",
            "AutoPolicy.IAutoPolicyAccessProvider",
            "AutoPolicy.IPermissionRegistry",
            "AutoPolicy.PageModelPermissionExtensions",
            "AutoPolicy.PermissionDeniedBehavior",
            "AutoPolicy.PermissionGroupBuilder",
            "AutoPolicy.PermissionRegistration",
            "AutoPolicy.PermissionRoleBuilder"
        };

        var actual = typeof(AutoPolicyAccess).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            actual);
    }
}
