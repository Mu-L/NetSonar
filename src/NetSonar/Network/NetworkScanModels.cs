using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace NetSonar.Avalonia.Network;

/// <summary>
/// Identifies which scan engine produced a result.
/// </summary>
public enum NetworkScanEngine
{
    [Description("nmap")]
    Nmap,

    [Description("Built-in")]
    Builtin,
}

/// <summary>
/// Classifies a host against the previous scan of the same target.
/// </summary>
public enum NetworkScannerChange
{
    [Description("")]
    Unchanged,

    [Description("New")]
    New,

    [Description("Ports changed")]
    PortsChanged,

    [Description("Gone")]
    Gone,
}

/// <summary>
/// The port range preset used by a port scan.
/// </summary>
public enum NetworkPortScanRange
{
    [Description("Top 100 ports")]
    Top100,

    [Description("Top 1000 ports")]
    Top1000,

    [Description("All ports (1-65535)")]
    Full,

    [Description("Custom")]
    Custom,
}

/// <summary>
/// Progress reported while a scan runs.
/// </summary>
/// <param name="Task">The task the engine is currently running.</param>
/// <param name="Percent">The completion percentage, from 0 to 100.</param>
/// <param name="Eta">The engine reported estimate of the remaining time, when known.</param>
public readonly record struct NetworkScanProgress(
    string Task,
    double Percent,
    TimeSpan? Eta = null
)
{
    public bool HasPercent => Percent is > 0 and <= 100;
}

/// <summary>
/// Options shared by every scan.
/// </summary>
public record NetworkScanOptions
{
    /// <summary>
    /// Gets a value indicating whether host names are resolved for the discovered addresses.
    /// </summary>
    public bool ResolveDns { get; init; } = true;

    /// <summary>
    /// Gets the nmap timing template (<c>-T0</c> to <c>-T5</c>).
    /// </summary>
    public int TimingTemplate { get; init; } = 4;

    /// <summary>
    /// Gets the per-host time budget, in seconds. Zero disables the limit.
    /// </summary>
    public int HostTimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Gets a value indicating whether the engine runs with administrator privileges.
    /// </summary>
    public bool Elevated { get; init; }

    /// <summary>
    /// Gets the per-probe timeout used by the built-in engine, in milliseconds.
    /// </summary>
    public int ProbeTimeoutMilliseconds { get; init; } = 1_000;

    /// <summary>
    /// Gets the maximum number of addresses a single target is allowed to expand to.
    /// </summary>
    public int MaxHosts { get; init; } = 4_096;

    /// <summary>
    /// Gets the reason shown on the platform authorization prompt of an elevated scan.
    /// </summary>
    public string ElevationPrompt { get; init; } = "Network scan";
}

/// <summary>
/// Options for a port scan.
/// </summary>
public sealed record NetworkPortScanOptions : NetworkScanOptions
{
    /// <summary>
    /// Gets the port range preset.
    /// </summary>
    public NetworkPortScanRange Range { get; init; } = NetworkPortScanRange.Top100;

    /// <summary>
    /// Gets the port specification used when <see cref="Range"/> is <see cref="NetworkPortScanRange.Custom"/>.
    /// </summary>
    public string CustomPorts { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the service and version of each open port is probed.
    /// </summary>
    public bool ServiceDetection { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether UDP ports are scanned. Requires <see cref="NetworkScanOptions.Elevated"/>.
    /// </summary>
    public bool UdpScan { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operating system is fingerprinted. Requires <see cref="NetworkScanOptions.Elevated"/>.
    /// </summary>
    public bool OsDetection { get; init; }
}

/// <summary>
/// An open port found on a host.
/// </summary>
public sealed class NetworkScannerPort
{
    public required int Number { get; init; }
    public required string Protocol { get; init; }
    public required string State { get; init; }
    public string Service { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string ExtraInfo { get; init; } = string.Empty;

    [JsonIgnore]
    public string ProductVersion =>
        string.Join(
            ' ',
            new[] { Product, Version, ExtraInfo }.Where(value => !string.IsNullOrWhiteSpace(value))
        );

    /// <summary>
    /// Gets the identity used to compare ports between two scans.
    /// </summary>
    [JsonIgnore]
    public string Key => $"{Number}/{Protocol}";
}

/// <summary>
/// A host found by a scan, with the open ports known for it.
/// </summary>
public sealed class NetworkScannerHost
{
    public required string Address { get; init; }
    public string Hostname { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string OperatingSystem { get; init; } = string.Empty;
    public DateTime LastSeen { get; init; } = DateTime.Now;
    public IReadOnlyList<NetworkScannerPort> Ports { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the host has been port scanned at least once.
    /// </summary>
    public bool IsPortScanned { get; init; }

    /// <summary>
    /// Gets how many consecutive scans failed to see this host.
    /// </summary>
    /// <remarks>
    /// Zero means the last scan saw it. A host is kept while this stays within the retention limit, so a single
    /// missed probe does not make it disappear from the table; see <see cref="NetworkScanRetention"/>.
    /// </remarks>
    public int MissedScans { get; init; }

    [JsonIgnore]
    public bool IsMissing => MissedScans > 0;

    [JsonIgnore]
    public int OpenPortCount => Ports.Count;

    /// <summary>
    /// Gets or sets how this host compares to the previous scan of the same target.
    /// </summary>
    [JsonIgnore]
    public NetworkScannerChange Change { get; set; }

    /// <summary>
    /// Returns a copy of this host carrying the port scan result of <paramref name="scanned"/>.
    /// </summary>
    /// <param name="scanned">The port scan result for the same address.</param>
    /// <returns>The merged host.</returns>
    public NetworkScannerHost WithPortScan(NetworkScannerHost? scanned)
    {
        return new NetworkScannerHost
        {
            Address = Address,
            Hostname = string.IsNullOrWhiteSpace(scanned?.Hostname) ? Hostname : scanned.Hostname,
            MacAddress = string.IsNullOrWhiteSpace(scanned?.MacAddress)
                ? MacAddress
                : scanned.MacAddress,
            Vendor = string.IsNullOrWhiteSpace(scanned?.Vendor) ? Vendor : scanned.Vendor,
            State = string.IsNullOrWhiteSpace(scanned?.State) ? State : scanned.State,
            OperatingSystem = string.IsNullOrWhiteSpace(scanned?.OperatingSystem)
                ? OperatingSystem
                : scanned.OperatingSystem,
            LastSeen = scanned?.LastSeen ?? LastSeen,
            // A host that answered discovery but reported nothing now has no open ports; keeping the
            // previous list would show ports that are no longer there.
            Ports = scanned?.Ports ?? [],
            IsPortScanned = true,
            MissedScans = scanned is null ? MissedScans : 0,
            Change = Change,
        };
    }

    /// <summary>
    /// Returns a copy of this host that keeps the port knowledge of a previous scan.
    /// </summary>
    /// <param name="previous">The same address in the previous scan, when it was seen before.</param>
    /// <returns>The merged host.</returns>
    /// <remarks>
    /// Host discovery reports no ports, so without this a rediscovery would erase every port the user already
    /// scanned. A port scan replaces the list instead; see <see cref="WithPortScan"/>.
    /// </remarks>
    public NetworkScannerHost WithCarriedPorts(NetworkScannerHost? previous)
    {
        if (previous is null || !previous.IsPortScanned || IsPortScanned) return this;

        return new NetworkScannerHost
        {
            Address = Address,
            Hostname = string.IsNullOrWhiteSpace(Hostname) ? previous.Hostname : Hostname,
            MacAddress = string.IsNullOrWhiteSpace(MacAddress) ? previous.MacAddress : MacAddress,
            Vendor = string.IsNullOrWhiteSpace(Vendor) ? previous.Vendor : Vendor,
            State = State,
            OperatingSystem = string.IsNullOrWhiteSpace(OperatingSystem)
                ? previous.OperatingSystem
                : OperatingSystem,
            LastSeen = LastSeen,
            Ports = previous.Ports,
            IsPortScanned = true,
            MissedScans = MissedScans,
            Change = Change,
        };
    }

    /// <summary>
    /// Returns a copy of this host carrying the link-layer identity found in the neighbour cache.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <param name="vendor">The vendor of that MAC address, when it is known.</param>
    /// <returns>The enriched host.</returns>
    public NetworkScannerHost WithNeighbour(string macAddress, string vendor)
    {
        return new NetworkScannerHost
        {
            Address = Address,
            Hostname = Hostname,
            MacAddress = macAddress,
            Vendor = string.IsNullOrWhiteSpace(vendor) ? Vendor : vendor,
            State = State,
            OperatingSystem = OperatingSystem,
            LastSeen = LastSeen,
            Ports = Ports,
            IsPortScanned = IsPortScanned,
            MissedScans = MissedScans,
            Change = Change,
        };
    }

    /// <summary>
    /// Returns a copy of this host counted as missing by one more scan.
    /// </summary>
    /// <returns>The host, marked as no longer answering.</returns>
    public NetworkScannerHost WithMissedScan()
    {
        return new NetworkScannerHost
        {
            Address = Address,
            Hostname = Hostname,
            MacAddress = MacAddress,
            Vendor = Vendor,
            State = "down",
            OperatingSystem = OperatingSystem,
            LastSeen = LastSeen,
            Ports = Ports,
            IsPortScanned = IsPortScanned,
            MissedScans = MissedScans + 1,
            Change = NetworkScannerChange.Gone,
        };
    }

    /// <summary>
    /// Returns the address as an <see cref="IPAddress"/>, or <see langword="null"/> when it is not a literal.
    /// </summary>
    public IPAddress? TryGetIpAddress()
    {
        return IPAddress.TryParse(Address, out var address) ? address : null;
    }

    /// <summary>
    /// Returns a sort key that orders IPv4 addresses numerically instead of lexically.
    /// </summary>
    public ulong GetSortKey()
    {
        var address = TryGetIpAddress();
        if (address is null) return ulong.MaxValue;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily != AddressFamily.InterNetwork) return ulong.MaxValue - 1;

        return ((ulong)bytes[0] << 24) | ((ulong)bytes[1] << 16) | ((ulong)bytes[2] << 8) | bytes[3];
    }
}

/// <summary>
/// The outcome of a scan.
/// </summary>
/// <param name="Hosts">The hosts the scan reported as up.</param>
/// <param name="Engine">The engine that produced the result.</param>
/// <param name="RawOutput">The raw engine output, when the engine produces one.</param>
public sealed record NetworkScanResult(
    IReadOnlyList<NetworkScannerHost> Hosts,
    NetworkScanEngine Engine,
    string RawOutput
)
{
    public static NetworkScanResult Empty(NetworkScanEngine engine) =>
        new([], engine, string.Empty);
}

/// <summary>
/// Keeps a host that a single scan failed to see, so a missed probe does not remove it from the table.
/// </summary>
/// <remarks>
/// Host discovery is a live probe: an unprivileged scan, an aggressive timing template, or a device in power
/// save all produce misses that do not mean the device left the network. Without this, every such miss shows
/// up as a removed host and its return as a new one.
/// </remarks>
public static class NetworkScanRetention
{
    /// <summary>
    /// The number of consecutive misses a host survives before it is dropped.
    /// </summary>
    public const int DefaultGraceScans = 3;

    /// <summary>
    /// Merges a fresh discovery with the hosts of the previous scan that it did not see.
    /// </summary>
    /// <param name="previous">The hosts of the previous scan.</param>
    /// <param name="current">The hosts the fresh discovery reported.</param>
    /// <param name="graceScans">How many consecutive misses a host survives.</param>
    /// <returns>The hosts to display, ordered by address.</returns>
    public static List<NetworkScannerHost> KeepRecentlySeen(
        IReadOnlyList<NetworkScannerHost> previous,
        IReadOnlyList<NetworkScannerHost> current,
        int graceScans = DefaultGraceScans
    )
    {
        var hosts = new List<NetworkScannerHost>(current.Count + previous.Count);
        hosts.AddRange(current);

        if (graceScans > 0 && previous.Count > 0)
        {
            var seen = current
                .Select(host => host.Address)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var host in previous)
            {
                if (seen.Contains(host.Address)) continue;

                var missing = host.WithMissedScan();
                if (missing.MissedScans > graceScans) continue;

                hosts.Add(missing);
            }
        }

        hosts.Sort((left, right) => left.GetSortKey().CompareTo(right.GetSortKey()));
        return hosts;
    }
}

/// <summary>
/// The difference between two scans of the same target.
/// </summary>
/// <param name="NewHosts">The addresses seen for the first time.</param>
/// <param name="GoneHosts">The addresses that stopped answering.</param>
/// <param name="ChangedHosts">The addresses whose open port set changed.</param>
public sealed record NetworkScanDiff(
    IReadOnlyList<string> NewHosts,
    IReadOnlyList<string> GoneHosts,
    IReadOnlyList<string> ChangedHosts
)
{
    public static readonly NetworkScanDiff Empty = new([], [], []);

    public bool HasChanges => NewHosts.Count > 0 || GoneHosts.Count > 0 || ChangedHosts.Count > 0;

    /// <summary>
    /// Compares a fresh scan against the previous one and tags every host in <paramref name="current"/>.
    /// </summary>
    /// <param name="previous">The hosts of the previous scan.</param>
    /// <param name="current">The hosts of the fresh scan. Their <see cref="NetworkScannerHost.Change"/> is updated.</param>
    /// <returns>The computed difference.</returns>
    public static NetworkScanDiff Compare(
        IReadOnlyList<NetworkScannerHost> previous,
        IReadOnlyList<NetworkScannerHost> current
    )
    {
        if (previous.Count == 0)
        {
            foreach (var host in current)
                host.Change = NetworkScannerChange.Unchanged;
            return Empty;
        }

        var previousByAddress = new Dictionary<string, NetworkScannerHost>(
            previous.Count,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var host in previous)
            previousByAddress[host.Address] = host;

        var newHosts = new List<string>();
        var changedHosts = new List<string>();
        var seen = new HashSet<string>(current.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var host in current)
        {
            seen.Add(host.Address);
            if (!previousByAddress.TryGetValue(host.Address, out var before))
            {
                host.Change = NetworkScannerChange.New;
                newHosts.Add(host.Address);
                continue;
            }

            if (host.IsPortScanned && before.IsPortScanned && HasPortChanges(before, host))
            {
                host.Change = NetworkScannerChange.PortsChanged;
                changedHosts.Add(host.Address);
                continue;
            }

            host.Change = NetworkScannerChange.Unchanged;
        }

        var goneHosts = previous
            .Where(host => !seen.Contains(host.Address))
            .Select(host => host.Address)
            .ToArray();

        return new NetworkScanDiff(newHosts, goneHosts, changedHosts);
    }

    private static bool HasPortChanges(NetworkScannerHost before, NetworkScannerHost after)
    {
        if (before.OpenPortCount != after.OpenPortCount) return true;

        var beforeKeys = before.Ports.Select(port => port.Key).ToHashSet(StringComparer.Ordinal);
        return after.Ports.Any(port => !beforeKeys.Contains(port.Key));
    }
}
