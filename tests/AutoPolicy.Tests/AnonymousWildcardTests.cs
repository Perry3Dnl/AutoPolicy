using Xunit;

namespace AutoPolicy.Tests;

public sealed class AnonymousWildcardTests
{
    [Fact]
    public void AllowAnonymous_RejectsMatchAllWildcard()
    {
        var options = new AutoPolicyOptions();

        var exception = Assert.Throws<ArgumentException>(
            () => options.AllowAnonymous(PermissionPattern.MatchAll));

        Assert.Contains("ProtectRazorPagesByDefault(false)", exception.Message);
        Assert.Empty(options.AnonymousPatterns);
    }

    [Fact]
    public void AllowAnonymous_AllowsScopedPrefixWildcard()
    {
        var options = new AutoPolicyOptions();

        options.AllowAnonymous("/Public/*");

        Assert.Contains("/Public/*", options.AnonymousPatterns);
    }
}
