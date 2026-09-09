using Xunit;

namespace AutoPolicy.Tests;

public sealed class PermissionEvaluatorTests
{
    [Fact]
    public void Evaluator_ConsumesAutoPolicyAccessWithoutIdentityDependency()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Reports/*"]
        };

        Assert.True(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Admin/Index", access));
    }

    [Fact]
    public void ExplicitDenyWinsOverRoleAllow()
    {
        var model = new PermissionModel();
        model.DefineRole("Staff", role => role.Include("/Staff/*"));

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            DenyPermissions = ["/Staff/Secret"]
        };

        Assert.True(evaluator.HasAccess("/Staff/Dashboard", access));
        Assert.False(evaluator.HasAccess("/Staff/Secret", access));
    }
}
