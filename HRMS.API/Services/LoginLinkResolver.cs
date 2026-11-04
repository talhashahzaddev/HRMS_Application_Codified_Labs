namespace HRMS.API.Services;

/// <summary>
/// Picks the login link for emails. When the request comes from a local frontend
/// (localhost, *.localhost or an IP), the link points to the local login page the same way
/// the frontend does (tenant.localhost → login.localhost, same port). Otherwise the configured
/// live login URL is used.
/// </summary>
public static class LoginLinkResolver
{
    public static string? Resolve(string? requestOrigin, string? configuredLoginUrl)
    {
        if (!Uri.TryCreate(requestOrigin, UriKind.Absolute, out var origin) ||
            (origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps))
        {
            return configuredLoginUrl;
        }

        var host = origin.Host.ToLowerInvariant();
        var isSubdomainOfLocalhost = host.EndsWith(".localhost");
        var isLocal = host == "localhost" || isSubdomainOfLocalhost || origin.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6;
        if (!isLocal)
        {
            return configuredLoginUrl;
        }

        var loginHost = isSubdomainOfLocalhost ? "login.localhost" : origin.Host;
        var port = origin.IsDefaultPort ? "" : $":{origin.Port}";
        return $"{origin.Scheme}://{loginHost}{port}/login";
    }
}
