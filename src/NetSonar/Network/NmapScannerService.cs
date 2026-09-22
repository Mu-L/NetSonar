using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using StageKit.Primitives.System;

namespace NetSonar.Avalonia.Network;

public sealed class NetworkScannerPort
{
    public required int Number { get; init; }
    public required string Protocol { get; init; }
    public required string State { get; init; }
    public string Service { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ExtraInfo { get; init; } = string.Empty;

    public string ProductVersion =>
        string.Join(
            ' ',
            new[] { Product, Version, ExtraInfo }.Where(value => !string.IsNullOrWhiteSpace(value))
        );
}

public sealed class NetworkScannerHost
{
    public required string Address { get; init; }
    public string Hostname { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public IReadOnlyList<NetworkScannerPort> Ports { get; init; } = [];
    public int OpenPortCount => Ports.Count;
}

public static class NmapScannerService
{
    public static bool TryFindExecutable(out string executablePath)
    {
        var executableName = HostSystem.NormalizeExecutableExtension("nmap");
        if (HostSystem.TryFindExecutable(executableName, out var foundPath))
        {
            executablePath = foundPath;
            return executablePath.Length > 0;
        }

        if (OperatingSystem.IsWindows())
        {
            var candidates = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Nmap",
                    "nmap.exe"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Nmap",
                    "nmap.exe"
                ),
            };
            executablePath = candidates.FirstOrDefault(File.Exists) ?? string.Empty;
            return executablePath.Length > 0;
        }

        executablePath = string.Empty;
        return false;
    }

    public static IReadOnlyList<string> GetLocalTargets()
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!NetworkInterfaceBridge.IsRealActiveInterface(networkInterface))
                continue;

            try
            {
                foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (
                        address.Address.AddressFamily != AddressFamily.InterNetwork
                        || IPAddress.IsLoopback(address.Address)
                        || NetworkInterfaceBridge.IsLinkLocal(address.Address)
                    )
                        continue;

                    targets.Add(GetNetworkCidr(address.Address, address.PrefixLength));
                }
            }
            catch (NetworkInformationException)
            {
                // A disappearing adapter cannot contribute a scan target.
            }
        }

        return targets.Order(StringComparer.Ordinal).ToArray();
    }

    public static string GetNetworkCidr(IPAddress address, int prefixLength)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || prefixLength is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(prefixLength));

        var bytes = address.GetAddressBytes();
        for (var bit = prefixLength; bit < 32; bit++)
        {
            bytes[bit / 8] &= (byte)~(0x80 >> (bit % 8));
        }

        return $"{new IPAddress(bytes)}/{prefixLength}";
    }

    public static bool IsValidTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return false;
        var value = target.Trim();
        if (value.StartsWith('-') || value.Any(char.IsWhiteSpace))
            return false;

        if (IPAddress.TryParse(value, out _))
            return true;
        if (TryParseCidr(value))
            return true;

        return Uri.CheckHostName(value)
                is UriHostNameType.Dns
                    or UriHostNameType.IPv4
                    or UriHostNameType.IPv6
            || IsIpv4OctetRange(value);
    }

    public static async Task<IReadOnlyList<NetworkScannerHost>> DiscoverAsync(
        string executablePath,
        string target,
        CancellationToken cancellationToken = default
    )
    {
        var output = await ProcessHelper.GetProcessOutputAsync(
            executablePath,
            ["-sn", "-oX", "-", target],
            false,
            cancellationToken
        );
        EnsureSucceeded(output.ExitCode, output.StandardError);
        return ParseXml(output.StandardOutput);
    }

    public static async Task<IReadOnlyList<NetworkScannerHost>> ScanPortsAsync(
        string executablePath,
        IEnumerable<string> hosts,
        CancellationToken cancellationToken = default
    )
    {
        var targetHosts = hosts.Distinct(StringComparer.Ordinal).ToArray();
        if (targetHosts.Length == 0)
            return [];

        var arguments = new List<string>
        {
            "-sT",
            "-sV",
            "--top-ports",
            "100",
            "--open",
            "-oX",
            "-",
        };
        arguments.AddRange(targetHosts);

        var output = await ProcessHelper.GetProcessOutputAsync(
            executablePath,
            arguments,
            false,
            cancellationToken
        );
        EnsureSucceeded(output.ExitCode, output.StandardError);
        return ParseXml(output.StandardOutput);
    }

    public static IReadOnlyList<NetworkScannerHost> ParseXml(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.None);
        return document
                .Root?.Elements("host")
                .Where(host =>
                    string.Equals(
                        (string?)host.Element("status")?.Attribute("state"),
                        "up",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Select(ParseHost)
                .Where(host => host is not null)
                .Cast<NetworkScannerHost>()
                .ToArray()
            ?? [];
    }

    private static NetworkScannerHost? ParseHost(XElement element)
    {
        var addresses = element.Elements("address").ToArray();
        var ipv4 = addresses.FirstOrDefault(address =>
            (string?)address.Attribute("addrtype") == "ipv4"
        );
        var ipv6 = addresses.FirstOrDefault(address =>
            (string?)address.Attribute("addrtype") == "ipv6"
        );
        var ipAddress = (string?)(ipv4 ?? ipv6)?.Attribute("addr");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        var mac = addresses.FirstOrDefault(address =>
            (string?)address.Attribute("addrtype") == "mac"
        );
        var ports =
            element
                .Element("ports")
                ?.Elements("port")
                .Where(port =>
                    string.Equals(
                        (string?)port.Element("state")?.Attribute("state"),
                        "open",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Select(port => new NetworkScannerPort
                {
                    Number = int.TryParse((string?)port.Attribute("portid"), out var number)
                        ? number
                        : 0,
                    Protocol = (string?)port.Attribute("protocol") ?? string.Empty,
                    State = (string?)port.Element("state")?.Attribute("state") ?? string.Empty,
                    Service = (string?)port.Element("service")?.Attribute("name") ?? string.Empty,
                    Product =
                        (string?)port.Element("service")?.Attribute("product") ?? string.Empty,
                    Version =
                        (string?)port.Element("service")?.Attribute("version") ?? string.Empty,
                    ExtraInfo =
                        (string?)port.Element("service")?.Attribute("extrainfo") ?? string.Empty,
                })
                .Where(port => port.Number > 0)
                .OrderBy(port => port.Number)
                .ToArray()
            ?? [];

        return new NetworkScannerHost
        {
            Address = ipAddress,
            Hostname =
                (string?)element.Element("hostnames")?.Element("hostname")?.Attribute("name")
                ?? string.Empty,
            MacAddress = (string?)mac?.Attribute("addr") ?? string.Empty,
            Vendor = (string?)mac?.Attribute("vendor") ?? string.Empty,
            State = (string?)element.Element("status")?.Attribute("state") ?? string.Empty,
            Ports = ports,
        };
    }

    private static void EnsureSucceeded(int exitCode, string standardError)
    {
        if (exitCode == 0)
            return;
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(standardError)
                ? $"nmap exited with code {exitCode}."
                : standardError.Trim()
        );
    }

    private static bool TryParseCidr(string value)
    {
        var separator = value.LastIndexOf('/');
        if (
            separator <= 0
            || !int.TryParse(value[(separator + 1)..], out var prefix)
            || !IPAddress.TryParse(value[..separator], out var address)
        )
            return false;

        return prefix >= 0
            && (address.AddressFamily == AddressFamily.InterNetwork ? prefix <= 32 : prefix <= 128);
    }

    private static bool IsIpv4OctetRange(string value)
    {
        var octets = value.Split('.');
        return octets.Length == 4
            && octets.All(octet => octet.Split(',').All(IsIpv4OctetRangePart));
    }

    private static bool IsIpv4OctetRangePart(string value)
    {
        var range = value.Split('-');
        if (range.Length is < 1 or > 2)
            return false;
        if (range.Length == 1)
            return byte.TryParse(value, out _);
        return range.All(part => part.Length == 0 || byte.TryParse(part, out _));
    }
}
