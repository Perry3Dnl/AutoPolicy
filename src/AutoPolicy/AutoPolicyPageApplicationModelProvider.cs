using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;

namespace AutoPolicy;

internal sealed class AutoPolicyPageApplicationModelProvider : IPageApplicationModelProvider
{
    private readonly PermissionRegistry _registry;
    private readonly IOptions<AutoPolicyOptions> _options;

    public AutoPolicyPageApplicationModelProvider(
        PermissionRegistry registry,
        IOptions<AutoPolicyOptions> options)
    {
        _registry = registry;
        _options = options;
    }

    public int Order => 1000;

    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        var model = context.PageApplicationModel;
        var options = _options.Value;
        var area = AutoPolicyKeyFactory.GetArea(model);
        var key = AutoPolicyKeyFactory.Create(model, options);

        var allowAnonymous = HasAllowAnonymous(model);
        var publicByConvention = AutoPolicyKeyFactory.MatchesAnonymousPattern(key, options);
        var optedIn = HasAttribute<AutoPolicyAttribute>(model);

        var shouldProtect = (options.RazorPagesProtectedByDefault || optedIn)
            && !allowAnonymous
            && !publicByConvention;

        var source = model.RelativePath ?? model.ViewEnginePath ?? key;
        _registry.TryAdd(
            new PermissionRegistration(key, model.ViewEnginePath, area, model.RelativePath, shouldProtect),
            source);

        if (!model.EndpointMetadata.OfType<AutoPolicyMetadata>().Any())
        {
            model.EndpointMetadata.Add(new AutoPolicyMetadata(key));
        }

        if (shouldProtect
            && !model.EndpointMetadata.OfType<AuthorizeAttribute>().Any(a =>
                a.Policy == AutoPolicyDefaults.PolicyName))
        {
            model.EndpointMetadata.Add(new AuthorizeAttribute(AutoPolicyDefaults.PolicyName));
        }
    }

    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }

    private static bool HasAllowAnonymous(PageApplicationModel model)
    {
        if (model.EndpointMetadata.OfType<IAllowAnonymous>().Any()
            || model.Filters.OfType<IAllowAnonymous>().Any()
            || model.Filters.OfType<AllowAnonymousFilter>().Any())
        {
            return true;
        }

        return HasAttribute<AllowAnonymousAttribute>(model);
    }

    private static bool HasAttribute<T>(PageApplicationModel model)
        where T : Attribute
    {
        if (model.EndpointMetadata.OfType<T>().Any())
        {
            return true;
        }

        return model.HandlerType.GetCustomAttributes<T>(inherit: true).Any();
    }
}
