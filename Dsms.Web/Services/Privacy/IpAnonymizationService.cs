using System.Net;
using System.Net.Sockets;
using Dsms.Web.Services.Logging;

namespace Dsms.Web.Services.Privacy;

public sealed class IpAnonymizationService : IIpAnonymizationService
{
    public string? AnonymizeIpAddress(string? ipAddress)
    {
        var first = LogIpAnonymizer.ExtractFirstIpAddress(ipAddress);
        if (string.IsNullOrWhiteSpace(first))
        {
            return null;
        }

        if (!IPAddress.TryParse(first, out var parsed))
        {
            return null;
        }

        if (parsed.IsIPv4MappedToIPv6)
        {
            parsed = parsed.MapToIPv4();
        }

        return parsed.AddressFamily switch
        {
            AddressFamily.InterNetwork => AnonymizeIPv4(parsed),
            AddressFamily.InterNetworkV6 => AnonymizeIPv6(parsed),
            _ => null
        };
    }

    private static string? AnonymizeIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return null;
        }

        bytes[3] = 0;
        return new IPAddress(bytes).ToString();
    }

    private static string? AnonymizeIPv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 16)
        {
            return null;
        }

        // Hintere 64 Bits (mindestens 80 Bits Anonymisierung gefordert – /64 erfüllt das) auf 0 setzen.
        for (var i = 8; i < 16; i++)
        {
            bytes[i] = 0;
        }

        return new IPAddress(bytes).ToString();
    }
}
