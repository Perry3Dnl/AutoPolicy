using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AutoPolicy;

internal static class AutoPolicyKeyFactory
{
    public static string Create(PageApplicationModel model, AutoPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Create(GetArea(model), model.ViewEnginePath, options);
    }

    public static string Create(string? area, string viewEnginePath, AutoPolicyOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(viewEnginePath);
        ArgumentNullException.ThrowIfNull(options);

        var path = viewEnginePath;
        if (!string.IsNullOrWhiteSpace(area))
        {
            var areaSegment = area.Trim('/');
            var page = viewEnginePath.StartsWith('/') ? viewEnginePath : "/" + viewEnginePath;
            path = "/" + areaSegment + page;
        }

        var canonical = PermissionKey.Normalize(path);
        if (options.PermissionKeyOverrides.TryGetValue(canonical, out var overridden))
        {
            return overridden;
        }

        return canonical;
    }

    public static bool MatchesAnonymousPattern(string permissionKey, AutoPolicyOptions options)
    {
        foreach (var pattern in options.AnonymousPatterns)
        {
            if (PermissionPattern.Matches(permissionKey, pattern))
            {
                return true;
            }
        }

        return false;
    }

    public static string? GetArea(PageApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var areaAttribute = model.HandlerType.GetCustomAttribute<AreaAttribute>(inherit: true);
        if (!string.IsNullOrWhiteSpace(areaAttribute?.RouteValue))
        {
            return areaAttribute.RouteValue;
        }

        var relativePath = model.RelativePath?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 3
            && segments[0].Equals("Areas", StringComparison.OrdinalIgnoreCase)
            && segments[2].Equals("Pages", StringComparison.OrdinalIgnoreCase))
        {
            return segments[1];
        }

        return null;
    }
}
