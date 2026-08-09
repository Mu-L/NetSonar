using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NetSonar.Avalonia.Extensions;

public static class IPAddressExtensions
{
    /// <summary>
    /// Parses a bare IP address or a bracketed IP literal without an endpoint port.
    /// </summary>
    public static bool TryParseLiteral(string? value, out IPAddress? ipAddress)
    {
        ipAddress = null;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var span = value.AsSpan();
        if (span[0] == '[')
        {
            if (span.Length < 2 || span[^1] != ']') return false;
            span = span[1..^1];
        }

        return IPAddress.TryParse(span, out ipAddress);
    }

    public static bool IsValid(this IPAddress ipAddress)
    {
        return !ipAddress.Equals(IPAddress.None) && !ipAddress.Equals(IPAddress.Any) && !ipAddress.Equals(IPAddress.IPv6Any);
    }

    public static bool IsValid(this IPEndPoint ipEndPoint)
    {
        return ipEndPoint.Address.IsValid();
    }

    /// <summary>
    /// Determines the IP version of the specified <see cref="IPAddress"/>.
    /// </summary>
    /// <param name="ipAddress">The <see cref="IPAddress"/> instance to evaluate. Cannot be <see langword="null"/>.</param>
    /// <returns>The IP version as an integer: <see langword="6"/> for IPv6 addresses, or <see langword="4"/> for IPv4 addresses.</returns>
    public static int GetIPVersion(this IPAddress ipAddress)
    {
        return ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 6 : 4;
    }

    /// <summary>
    /// Returns a string representation of the IP version for the specified <see cref="IPAddress"/>.
    /// </summary>
    /// <param name="ipAddress">The <see cref="IPAddress"/> instance for which the IP version string is generated.</param>
    /// <returns>A string indicating the IP version, formatted as "IPv4" for IPv4 addresses or "IPv6" for IPv6 addresses.</returns>
    public static string GetIPVersionStr(this IPAddress ipAddress)
    {
        return $"IPv{ipAddress.GetIPVersion()}";
    }

    /// <summary>
    /// Convert a CIDR prefix length to a subnet mask "255.255.255.0" format.
    /// </summary>
    public static IPAddress CidrToMask(int cidr, AddressFamily family = AddressFamily.InterNetwork)
    {
        Debug.Assert(cidr is >= 0 and <= 126);
        Debug.Assert(family is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);

        var addressBytes = (family == AddressFamily.InterNetwork) ?
            stackalloc byte[4] :
            stackalloc byte[16];
        addressBytes.Clear();

        Debug.Assert(cidr <= (addressBytes.Length * 8));

        // Enable bits one at a time from left/high to right/low.
        for (int bit = 0; bit < cidr; bit++)
        {
            addressBytes[bit / 8] |= (byte)(0x80 >> (bit % 8));
        }

        return new IPAddress(addressBytes);

        /*var mask = cidr == 0 ? 0 : uint.MaxValue << (32 - cidr);
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, mask);
        return new IPAddress(bytes);*/
    }

    /// <summary>
    /// Converts a subnet mask to a CIDR prefix length.
    /// </summary>
    /// <param name="subnetMask"></param>
    /// <returns></returns>
    public static int MaskToCidr(IPAddress subnetMask)
    {
        var bytes = subnetMask.GetAddressBytes();
        var cidr = 0;
        foreach (var t in bytes)
        {
            var b = t;
            while (b > 0)
            {
                cidr++;
                b = (byte)(b << 1);
            }
        }
        return cidr;
    }
}
