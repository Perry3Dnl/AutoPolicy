using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

public static class AutoPolicyServiceCollectionExtensions
{
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
        services.AddSingleton<PermissionRegistry>();
        services.AddSingleton<IPermissionRegistry>(sp => sp.GetRequiredService<PermissionRegistry>());
        services.AddSingleton<IPermissionEvaluator>(sp =>
            new PermissionEvaluator(sp.GetRequiredService<PermissionModel>()));

        services.TryAddScoped<IAutoPolicyAccessProvider, ClaimsAutoPolicyAccessProvider>();

        services.AddTransient<IAuthorizationHandler, AutoPolicyHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AutoPolicyDefaults.PolicyName, policy =>
            {
                policy.AddRequirements(new AutoPolicyRequirement());
            });
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, AutoPolicyPageApplicationModelProvider>());
        services.AddHostedService<AutoPolicyStartupValidator>();
        return services;
    }
}
