namespace HRMS.API.Configuration;

public static class CorsOriginHelper
{
    public static bool IsOriginAllowed(
        string? origin,
        IReadOnlyList<string> configuredOrigins,
        IReadOnlyList<string> allowedHostSuffixes)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        if (configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var suffix in allowedHostSuffixes)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                continue;
            }

            var normalized = suffix.Trim();
            if (!normalized.StartsWith('.'))
            {
                normalized = "." + normalized;
            }

            if (host.Equals(normalized.TrimStart('.'), StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
