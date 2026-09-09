using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

/// <summary>
/// Registers AutoPolicy with an ASP.NET Core application's dependency-injection container.
/// </summary>
public static class AutoPolicyServiceCollectionExtensions
{
    /// <summary>
    /// Adds automatic Razor Page permission discovery, authorization handling, startup validation,
    /// in-page permission services, and the built-in claims access provider fallback.
    /// </summary>
    /// <remarks>
    /// Register a custom <see cref="IAutoPolicyAccessProvider"/> before calling this method when the
    /// application loads access from its own database, session, cache, or other storage.
    /// </remarks>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">Optional AutoPolicy configuration.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddAutoPolicy(
        this IServiceCollection services,
        Action<AutoPolicyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AutoPolicyOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AutoPolicyOptions>>().Value.Model);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AutoPolicyOptions>>().Value;
            var registry = new PermissionRegistry();

            foreach (var permissionKey in options.ExplicitPermissions)
            {
                registry.TryAdd(
                    new PermissionRegistration(permissionKey),
                    $"explicit permission '{permissionKey}'");
            }

            return registry;
        });
        services.AddSingleton<IPermissionRegistry>(sp => sp.GetRequiredService<PermissionRegistry>());
        services.AddSingleton<IPermissionEvaluator>(sp =>
            new PermissionEvaluator(sp.GetRequiredService<PermissionModel>()));

        // The host application owns identity and assignment storage. Claims are only the built-in
        // fallback adapter and are not required when the application supplies its own provider.
        services.TryAddScoped<IAutoPolicyAccessProvider, ClaimsAutoPolicyAccessProvider>();

        services.AddTransient<IAuthorizationHandler, AutoPolicyHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AutoPolicyDefaults.PolicyName, policy =>
            {
                policy.AddRequirements(new AutoPolicyRequirement());
            });
        });

        DecorateAuthorizationResultHandler(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, AutoPolicyPageApplicationModelProvider>());
        services.AddHostedService<AutoPolicyStartupValidator>();
        return services;
    }

    private static void DecorateAuthorizationResultHandler(IServiceCollection services)
    {
        var existing = services.LastOrDefault(descriptor =>
            descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler)
            && !descriptor.IsKeyedService);

        if (existing is not null)
        {
            services.Remove(existing);
        }

        var lifetime = existing?.Lifetime ?? ServiceLifetime.Singleton;
        services.Add(ServiceDescriptor.Describe(
            typeof(IAuthorizationMiddlewareResultHandler),
            serviceProvider => new AutoPolicyAuthorizationMiddlewareResultHandler(
                serviceProvider.GetRequiredService<IOptions<AutoPolicyOptions>>(),
                ResolveAuthorizationResultHandler(existing, serviceProvider)),
            lifetime));
    }

    private static IAuthorizationMiddlewareResultHandler ResolveAuthorizationResultHandler(
        ServiceDescriptor? descriptor,
        IServiceProvider serviceProvider)
    {
        if (descriptor is null)
        {
            return new AuthorizationMiddlewareResultHandler();
        }

        if (descriptor.ImplementationInstance is IAuthorizationMiddlewareResultHandler instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (IAuthorizationMiddlewareResultHandler)descriptor.ImplementationFactory(serviceProvider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return (IAuthorizationMiddlewareResultHandler)ActivatorUtilities.GetServiceOrCreateInstance(
                serviceProvider,
                descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            "The existing IAuthorizationMiddlewareResultHandler registration could not be decorated.");
    }
}
