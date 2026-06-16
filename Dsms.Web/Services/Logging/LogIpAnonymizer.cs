using System.Net;
using System.Net.Sockets;

namespace Dsms.Web.Services.Logging;

/// <summary>
/// Anonymisiert IP-Adressen vor der Speicherung in Logeinträgen.
/// IPv4: letztes Oktett wird durch „xxx“ ersetzt.
/// IPv6: nur die ersten vier Segmente bleiben, Rest wird maskiert.
/// </summary>
public static class LogIpAnonymizer
{
    /// <summary>Erste IP aus einem Rohwert (z. B. X-Forwarded-For mit mehreren Adressen).</summary>
    public static string? ExtractFirstIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var trimmed = ipAddress.Trim();
        var first = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(first) ? null : first;
    }

    public static string? AnonymizeIpAddress(string? ipAddress)
    {
        var first = ExtractFirstIpAddress(ipAddress);
        if (string.IsNullOrWhiteSpace(first))
        {
            return null;
        }

        if (IPAddress.TryParse(first, out var parsed))
        {
            if (parsed.AddressFamily == AddressFamily.InterNetwork)
            {
                return AnonymizeIPv4(parsed);
            }

            if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return AnonymizeIPv6(parsed);
            }
        }

        return "IP anonymisiert";
    }

    public static string? GetClientIpAddress(Microsoft.AspNetCore.Http.HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string AnonymizeIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return "IP anonymisiert";
        }

        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.xxx";
    }

    private static string AnonymizeIPv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 16)
        {
            return "IPv6 anonymisiert";
        }

        var segments = new ushort[8];
        for (var i = 0; i < 8; i++)
        {
            segments[i] = (ushort)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
        }

        return $"{segments[0]:x4}:{segments[1]:x4}:{segments[2]:x4}:{segments[3]:x4}:xxxx:xxxx:xxxx:xxxx";
    }
}
