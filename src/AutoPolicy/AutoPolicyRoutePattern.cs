using System.Text;

namespace AutoPolicy;

internal static class AutoPolicyRoutePattern
{
    public static string ToPermissionKey(string? routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        var normalized = new StringBuilder(routePattern.Length);
        var insideParameter = false;
        var atParameterStart = false;

        for (var index = 0; index < routePattern.Length; index++)
        {
            var character = routePattern[index];
            if (character == '{')
            {
                insideParameter = true;
                atParameterStart = true;
                normalized.Append(character);
                continue;
            }

            if (insideParameter && atParameterStart && character == '*')
            {
                // Catch-all markers are routing syntax, not part of a permission identity.
                continue;
            }

            if (insideParameter && character == '?' && index + 1 < routePattern.Length
                && routePattern[index + 1] == '}')
            {
                // Optional markers are routing syntax, not part of a permission identity.
                continue;
            }

            if (insideParameter)
            {
                atParameterStart = false;
                normalized.Append(character switch
                {
                    '*' => "%2A",
                    '?' => "%3F",
                    '#' => "%23",
                    _ => character.ToString()
                });
            }
            else
            {
                normalized.Append(character);
            }

            if (character == '}')
            {
                insideParameter = false;
                atParameterStart = false;
            }
        }

        return PermissionKey.Normalize(normalized.ToString());
    }
}
