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
        var extraKeys = new List<string>(options.PermissionKeyOverrides.Values);
        var aliasErrors = new List<string>();

        foreach (var alias in options.Aliases)
        {
            try
            {
                extraKeys.Add(AutoPolicyPermissionKeyResolver.Resolve(alias.Key, options));
            }
            catch (Exception ex)
            {
                aliasErrors.Add($"Permission alias '{alias.Key}' is invalid: {ex.Message}");
            }
        }

        var result = PermissionModelValidator.Validate(
            options.Model,
            _registry,
            extraKeys,
            options.StrictValidation);

        foreach (var aliasError in aliasErrors)
        {
            result.AddError(aliasError);
        }

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
}
