using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Caching.Memory;

namespace HRMS.API.Services.PayslipLayouts;

/// <summary>
/// Downloads the payslip logo once (cached) with SSRF, size and timeout guards.
/// The logo URL is admin-supplied, so it must never be able to reach loopback / private / link-local
/// addresses (cloud metadata endpoints, internal services) or local files.
/// </summary>
public static class PayslipLogoLoader
{
    private const int MaxLogoBytes = 2 * 1024 * 1024;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var allowed = addresses.FirstOrDefault(IsPublicAddress)
                ?? throw new HttpRequestException("Logo host resolves to a non-public address.");

            var socket = new Socket(allowed.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(allowed, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    })
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    /// <summary>True when the value is an absolute http(s) URL (the only form accepted for payslip logos).</summary>
    public static bool IsAllowedLogoUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    public static async Task<byte[]?> LoadAsync(string? url, IMemoryCache? cache, ILogger? logger = null)
    {
        if (!IsAllowedLogoUrl(url))
        {
            return null;
        }

        var key = "payslip-logo:" + url!.Trim();
        if (cache != null && cache.TryGetValue(key, out byte[]? cached))
        {
            return cached;
        }

        byte[]? bytes = null;
        try
        {
            using var response = await Client.GetAsync(url.Trim(), HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode &&
                (response.Content.Headers.ContentLength ?? 0) <= MaxLogoBytes)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(chunk)) > 0)
                {
                    if (buffer.Length + read > MaxLogoBytes)
                    {
                        buffer.SetLength(0);
                        break;
                    }

                    buffer.Write(chunk, 0, read);
                }

                bytes = buffer.Length > 0 ? buffer.ToArray() : null;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Could not download payslip logo; rendering without it.");
            bytes = null;
        }

        // Failures are cached too so a bad URL does not slow down every payslip in a bulk run.
        cache?.Set(key, bytes, CacheDuration);
        return bytes;
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 10
                     || b[0] == 0
                     || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) // carrier-grade NAT
                     || (b[0] == 169 && b[1] == 254)               // link-local / cloud metadata
                     || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                     || (b[0] == 192 && b[1] == 168)
                     || b[0] >= 224);                               // multicast / reserved
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var b = address.GetAddressBytes();
            return !(address.IsIPv6LinkLocal
                     || address.IsIPv6SiteLocal
                     || address.IsIPv6Multicast
                     || (b[0] & 0xFE) == 0xFC); // unique local fc00::/7
        }

        return false;
    }
}
