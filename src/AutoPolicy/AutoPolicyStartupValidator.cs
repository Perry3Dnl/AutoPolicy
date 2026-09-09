using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

internal sealed class AutoPolicyStartupValidator : IHostedService
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly PermissionRegistry _registry;
    private readonly IOptions<AutoPolicyOptions> _options;
    private readonly ILogger<AutoPolicyStartupValidator> _logger;

    public AutoPolicyStartupValidator(
        EndpointDataSource endpointDataSource,
        PermissionRegistry registry,
        IOptions<AutoPolicyOptions> options,
        ILogger<AutoPolicyStartupValidator> logger)
    {
        _endpointDataSource = endpointDataSource;
        _registry = registry;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = _endpointDataSource.Endpoints;

        var options = _options.Value;
        var result = PermissionModelValidator.Validate(
            options.Model,
            _registry,
            strict: options.StrictValidation);

        ValidateOverrides(options, result);
        ValidateAliases(options, result);

        foreach (var warning in result.Warnings)
        {
            _logger.LogWarning("{AutoPolicyWarning}", warning);
        }

        foreach (var error in result.Errors)
        {
            _logger.LogError("{AutoPolicyError}", error);
        }

        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "AutoPolicy startup validation failed:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, result.Errors));
        }

        var registrations = _registry.GetAll();
        _logger.LogInformation(
            "AutoPolicy registered {PermissionCount} permissions, {ProtectedPageCount} protected Razor Pages, {ExplicitPermissionCount} explicit capabilities, {DuplicateCount} duplicate keys, {WarningCount} validation warnings.",
            registrations.Count,
            registrations.Count(r => r.IsProtected),
            options.ExplicitPermissions.Count,
            _registry.Duplicates.Count,
            result.Warnings.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateOverrides(AutoPolicyOptions options, PermissionValidationResult result)
    {
        var physicalPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in _registry.GetAll())
        {
            if (!string.IsNullOrWhiteSpace(registration.ViewEnginePath))
            {
                physicalPages.Add(GetPhysicalPageKey(registration));
            }
        }

        foreach (var permissionOverride in options.PermissionKeyOverrides)
        {
            if (!physicalPages.Contains(permissionOverride.Key))
            {
                result.AddError(
                    $"Permission override source '{permissionOverride.Key}' does not map to a discovered Razor Page.");
                continue;
            }

            if (!_registry.Contains(permissionOverride.Value))
            {
                result.AddError(
                    $"Permission override '{permissionOverride.Key}' resolves to '{permissionOverride.Value}', "
                    + "but that canonical permission is not registered.");
            }
        }
    }

    private void ValidateAliases(AutoPolicyOptions options, PermissionValidationResult result)
    {
        foreach (var alias in options.Aliases)
        {
            if (_registry.Contains(alias.Key))
            {
                result.AddError(
                    $"Permission alias source '{alias.Key}' is already a registered permission. "
                    + "Use OverridePermissionKey(...) when intentionally remapping a Razor Page permission.");
                continue;
            }

            string resolved;
            try
            {
                resolved = AutoPolicyPermissionKeyResolver.Resolve(alias.Key, options);
            }
            catch (Exception ex)
            {
                result.AddError($"Permission alias '{alias.Key}' is invalid: {ex.Message}");
                continue;
            }

            if (!_registry.Contains(resolved))
            {
                result.AddError(
                    $"Permission alias '{alias.Key}' resolves to '{resolved}', which does not resolve to a registered permission.");
            }
        }
    }

    private static string GetPhysicalPageKey(PermissionRegistration registration)
    {
        var page = registration.ViewEnginePath!;
        if (string.IsNullOrWhiteSpace(registration.Area))
        {
            return PermissionKey.Normalize(page);
        }

        var area = registration.Area.Trim('/');
        var pagePath = page.StartsWith('/') ? page : "/" + page;
        return PermissionKey.Normalize("/" + area + pagePath);
    }
}
