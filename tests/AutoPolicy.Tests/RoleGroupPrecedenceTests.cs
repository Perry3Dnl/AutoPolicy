using Xunit;

namespace AutoPolicy.Tests;

public sealed class RoleGroupPrecedenceTests
{
    [Fact]
    public void AllowRole_ExpandsDirectPatternsAndNestedGroups()
    {
        var model = new PermissionModel();
        model.DefineGroup("ReportRead", group => group.Include("/Reports/View"));
        model.DefineGroup("ReportManage", group =>
        {
            group.IncludeGroup("ReportRead");
            group.Include("/Reports/Edit");
        });
        model.DefineRole("Staff", role =>
        {
            role.IncludeGroup("ReportManage");
            role.Include("/Profile");
        });

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess { AllowRoles = ["Staff"] };

        Assert.True(evaluator.HasAccess("/Reports/View", access));
        Assert.True(evaluator.HasAccess("/Reports/Edit", access));
        Assert.True(evaluator.HasAccess("/Profile", access));
        Assert.False(evaluator.HasAccess("/Administration", access));
    }

    [Fact]
    public void NestedGroups_WithDiamondShape_DoNotProduceFalseCycle()
    {
        var model = new PermissionModel();
        model.DefineGroup("Common", group => group.Include("/Common"));
        model.DefineGroup("Left", group => group.IncludeGroup("Common"));
        model.DefineGroup("Right", group => group.IncludeGroup("Common"));
        model.DefineGroup("Root", group =>
        {
            group.IncludeGroup("Left");
            group.IncludeGroup("Right");
        });

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess { AllowGroups = ["Root"] };

        Assert.True(evaluator.HasAccess("/Common", access));
    }

    [Fact]
    public void DenyGroup_OverridesPermissionGrantedByAllowRole()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            DenyGroups = ["Reports"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.True(evaluator.HasAccess("/Members/List", access));
    }

    [Fact]
    public void DenyRole_OverridesDirectPermissionGrant()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Reports/Daily"],
            DenyRoles = ["Staff"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void DenyRole_OverridesAllowGroupGrant()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowGroups = ["Reports"],
            DenyRoles = ["Staff"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void DenyGroup_OverridesDirectPermissionGrant()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Reports/Daily"],
            DenyGroups = ["Reports"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void SameRoleInAllowAndDeny_IsDenied()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            DenyRoles = ["Staff"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Members/List", access));
    }

    [Fact]
    public void SameGroupInAllowAndDeny_IsDenied()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowGroups = ["Reports"],
            DenyGroups = ["Reports"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void ExactDeny_OverridesBroadAllowWildcardWithoutAffectingSibling()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Reports/*"],
            DenyPermissions = ["/Reports/Secret"]
        };

        Assert.True(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Reports/Secret", access));
    }

    [Fact]
    public void BroadDenyWildcard_OverridesNarrowAllowWildcard()
    {
        var evaluator = new PermissionEvaluator(new PermissionModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Admin/Reports/*"],
            DenyPermissions = ["/Admin/*"]
        };

        Assert.False(evaluator.HasAccess("/Admin/Reports/Daily", access));
    }

    [Fact]
    public void MultipleAllowSources_CannotOutvoteSingleDeny()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            AllowGroups = ["Reports"],
            AllowPermissions = ["/Reports/Daily"],
            DenyPermissions = ["/Reports/Daily"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void MatchAllRole_IsStillReducedByDenyGroup()
    {
        var model = BuildStaffModel();
        model.DefineRole("Administrator", role => role.Include(PermissionPattern.MatchAll));

        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Administrator"],
            DenyGroups = ["Reports"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.True(evaluator.HasAccess("/Members/List", access));
    }

    [Fact]
    public void RoleGroupAndPermissionNames_AreCaseInsensitive()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["sTaFf"],
            DenyGroups = ["rEpOrTs"]
        };

        Assert.False(evaluator.HasAccess("/REPORTS/DAILY", access));
        Assert.True(evaluator.HasAccess("/MEMBERS/LIST", access));
    }

    [Fact]
    public void UnknownAllowRoleAndGroup_GrantNothing()
    {
        var evaluator = new PermissionEvaluator(BuildStaffModel());
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["DoesNotExist"],
            AllowGroups = ["AlsoMissing"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Members/List", access));
    }

    [Fact]
    public void UnknownDenyRoleAndGroup_DoNotBecomeBlanketDenies()
    {
        var evaluator = new PermissionEvaluator(BuildStaffModel());
        var access = new AutoPolicyAccess
        {
            AllowPermissions = ["/Reports/Daily"],
            DenyRoles = ["DoesNotExist"],
            DenyGroups = ["AlsoMissing"]
        };

        Assert.True(evaluator.HasAccess("/Reports/Daily", access));
    }

    [Fact]
    public void EmptyAccess_DeniesEverything()
    {
        var evaluator = new PermissionEvaluator(BuildStaffModel());

        Assert.False(evaluator.HasAccess("/Reports/Daily", AutoPolicyAccess.Empty));
    }

    [Fact]
    public void DenyOnlySnapshot_NeverCreatesAnAllow()
    {
        var evaluator = new PermissionEvaluator(BuildStaffModel());
        var access = new AutoPolicyAccess
        {
            DenyGroups = ["Reports"]
        };

        Assert.False(evaluator.HasAccess("/Reports/Daily", access));
        Assert.False(evaluator.HasAccess("/Members/List", access));
    }

    [Fact]
    public void EffectivePermissions_ReflectRoleGrantAndGroupDeny()
    {
        var model = BuildStaffModel();
        var evaluator = new PermissionEvaluator(model);
        var registry = CreateRegistry(
            "/Reports/Daily",
            "/Reports/Secret",
            "/Members/List");
        var access = new AutoPolicyAccess
        {
            AllowRoles = ["Staff"],
            DenyGroups = ["Reports"]
        };

        var effective = evaluator.GetEffectivePermissions(access, registry);

        Assert.DoesNotContain("/Reports/Daily", effective);
        Assert.DoesNotContain("/Reports/Secret", effective);
        Assert.Contains("/Members/List", effective);
        Assert.Single(effective);
    }

    [Fact]
    public void CyclicNestedGroups_AreRejectedByValidation()
    {
        var model = new PermissionModel();
        model.DefineGroup("A", group => group.IncludeGroup("B"));
        model.DefineGroup("B", group => group.IncludeGroup("A"));

        var result = PermissionModelValidator.Validate(
            model,
            CreateRegistry("/Known"),
            strict: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.Contains("cyclic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnknownIncludedGroup_IsWarningNormallyAndErrorInStrictMode()
    {
        var model = new PermissionModel();
        model.DefineGroup("Known", group => group.IncludeGroup("Missing"));
        var registry = CreateRegistry("/Known");

        var normal = PermissionModelValidator.Validate(model, registry, strict: false);
        var strict = PermissionModelValidator.Validate(model, registry, strict: true);

        Assert.Contains(normal.Warnings, warning => warning.Contains("Missing", StringComparison.Ordinal));
        Assert.DoesNotContain(normal.Errors, error => error.Contains("Missing", StringComparison.Ordinal));
        Assert.Contains(strict.Errors, error => error.Contains("Missing", StringComparison.Ordinal));
    }

    private static PermissionModel BuildStaffModel()
    {
        var model = new PermissionModel();
        model.DefineGroup("Reports", group => group.Include("/Reports/*"));
        model.DefineGroup("Members", group => group.Include("/Members/*"));
        model.DefineGroup("StaffBase", group =>
        {
            group.IncludeGroup("Reports");
            group.IncludeGroup("Members");
        });
        model.DefineRole("Staff", role => role.IncludeGroup("StaffBase"));
        return model;
    }

    private static PermissionRegistry CreateRegistry(params string[] keys)
    {
        var registry = new PermissionRegistry();
        foreach (var key in keys)
        {
            registry.TryAdd(new PermissionRegistration(key), "test");
        }

        return registry;
    }
}
