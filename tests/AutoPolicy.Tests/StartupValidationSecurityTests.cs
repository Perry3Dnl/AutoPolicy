using AutoPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutoPolicy.Tests;

public sealed class StartupValidationSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StartupValidationSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ConflictingAliasDefinitions_AreRejectedImmediately()
    {
        var options = new AutoPolicyOptions();
        options.AddAlias("/Legacy", "/Probe");
        options.AddAlias("/Legacy", "/Probe");

        var exception = Assert.Throws<InvalidOperationException>(
            () => options.AddAlias("/Legacy", "/Nested/Detail"));

        Assert.Contains("already maps", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConflictingPermissionOverrides_AreRejectedImmediately()
    {
        var options = new AutoPolicyOptions();
        options.OverridePermissionKey("/Probe", "/Virtual/Probe");
        options.OverridePermissionKey("/Probe", "/Virtual/Probe");

        var exception = Assert.Throws<InvalidOperationException>(
            () => options.OverridePermissionKey("/Probe", "/Other/Probe"));

        Assert.Contains("already maps", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateGroupDefinition_IsRejectedImmediately()
    {
        var options = new AutoPolicyOptions();
        options.DefineGroup("Staff", group => group.Include("/Probe"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => options.DefineGroup("staff", group => group.Include("/Nested/*")));

        Assert.Contains("already been defined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateRoleDefinition_IsRejectedImmediately()
    {
        var options = new AutoPolicyOptions();
        options.DefineRole("Admin", role => role.Include("*"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => options.DefineRole("ADMIN", role => role.Include("/Probe")));

        Assert.Contains("already been defined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelfReferencingAlias_IsRejectedImmediately()
    {
        var options = new AutoPolicyOptions();

        Assert.Throws<ArgumentException>(() => options.AddAlias("/Probe", "/Probe"));
    }

    [Fact]
    public void StaleExactPattern_IsReportedEvenWhenRegistryIsEmpty()
    {
        var model = new PermissionModel();
        model.DefineGroup("Missing", group => group.Include("/Does/Not/Exist"));

        var result = PermissionModelValidator.Validate(
            model,
            new PermissionRegistry(),
            strict: false);

        Assert.Contains(result.Warnings, warning =>
            warning.Contains("/Does/Not/Exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnknownIncludedGroup_IsStructuralErrorEvenWithoutStrictMode()
    {
        var model = new PermissionModel();
        model.DefineGroup("Known", group => group.IncludeGroup("Missing"));

        var result = PermissionModelValidator.Validate(
            model,
            new PermissionRegistry(),
            strict: false);

        Assert.Contains(result.Errors, error =>
            error.Contains("Missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AliasTargetThatDoesNotExist_FailsApplicationStartup()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                    options.AddAlias("/Legacy/Probe", "/Does/Not/Exist")));
        });

        var exception = await StartAndCaptureAsync(factory);

        Assert.Contains("alias", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not resolve to a registered permission", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AliasSourceCannotReplaceARegisteredPermission()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                    options.AddAlias("/Probe", "/Nested/Detail")));
        });

        var exception = await StartAndCaptureAsync(factory);

        Assert.Contains("alias source", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("registered permission", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OverrideForUnknownPhysicalPage_FailsApplicationStartup()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                    options.OverridePermissionKey("/Missing/Page", "/Probe")));
        });

        var exception = await StartAndCaptureAsync(factory);

        Assert.Contains("override source", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not map to a discovered Razor Page", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitPermissionCannotCollideWithDiscoveredPage()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
                services.PostConfigure<AutoPolicyOptions>(options =>
                    options.DefinePermission("/Probe")));
        });

        var exception = await StartAndCaptureAsync(factory);

        Assert.Contains("Duplicate canonical permission key", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Exception> StartAndCaptureAsync(WebApplicationFactory<Program> factory)
    {
        return await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = factory.CreateClient();
            _ = await client.GetAsync("/");
        });
    }
}
