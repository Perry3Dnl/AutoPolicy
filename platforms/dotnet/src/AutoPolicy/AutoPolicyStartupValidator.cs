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
        var extraKeys = options.PermissionKeyOverrides.Values.Concat(options.Aliases.Keys);
        var result = PermissionModelValidator.Validate(
            options.Model,
            _registry,
            extraKeys,
            options.StrictValidation);

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
            "AutoPolicy discovered {PageCount} Razor Pages, {ProtectedCount} protected, {DuplicateCount} duplicate keys, {WarningCount} validation warnings.",
            registrations.Count,
            registrations.Count(r => r.IsProtected),
            _registry.Duplicates.Count,
            result.Warnings.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
