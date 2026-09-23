using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetSonar.Avalonia.Network;

/// <summary>
/// Completes a scan result with the operating system neighbour (ARP) cache.
/// </summary>
/// <remarks>
/// An unprivileged nmap cannot ARP-ping, so it falls back to TCP connect pings and reports a host as down
/// whenever those probes miss; the host set then changes between two scans of the same network. The neighbour
/// cache holds the devices the machine actually exchanged packets with, which both stabilizes the host list
/// and supplies the MAC address an unprivileged scan never sees.
/// The cache trails reality, so a device that just left the network can survive here until its entry expires.
/// </remarks>
public static class NeighbourCacheMerge
{
    /// <summary>
    /// Adds the neighbour cache entries that belong to the scanned target and fills in missing MAC addresses.
    /// </summary>
    /// <param name="hosts">The hosts the engine reported.</param>
    /// <param name="target">The scanned target, used to reject cache entries from other networks.</param>
    /// <param name="options">The scan options.</param>
    /// <param name="cancellationToken">Cancels the merge.</param>
    /// <returns>The merged hosts, ordered by address.</returns>
    public static async Task<IReadOnlyList<NetworkScannerHost>> ApplyAsync(
        IReadOnlyList<NetworkScannerHost> hosts,
        string target,
        NetworkScanOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (!NetworkTarget.TryParse(target, out var networkTarget)) return hosts;

        var neighbours = await ArpTable.GetAsync(cancellationToken);
        if (neighbours.Count == 0) return hosts;

        var merged = new List<NetworkScannerHost>(hosts.Count);
        var known = new HashSet<string>(hosts.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var host in hosts)
        {
            known.Add(host.Address);

            var macAddress = neighbours.GetValueOrDefault(host.Address, string.Empty);
            merged.Add(
                macAddress.Length == 0 || host.MacAddress.Length > 0
                    ? host
                    : host.WithNeighbour(macAddress, MacVendorLookup.Resolve(macAddress))
            );
        }

        foreach (var neighbour in neighbours)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (known.Contains(neighbour.Key) || !networkTarget.Contains(neighbour.Key)) continue;

            merged.Add(
                new NetworkScannerHost
                {
                    Address = neighbour.Key,
                    Hostname = options.ResolveDns
                        ? await NetworkHostResolver.ResolveHostNameAsync(
                            neighbour.Key,
                            cancellationToken
                        )
                        : string.Empty,
                    MacAddress = neighbour.Value,
                    Vendor = MacVendorLookup.Resolve(neighbour.Value),
                    State = "up",
                    LastSeen = DateTime.Now,
                }
            );
        }

        return merged.OrderBy(host => host.GetSortKey()).ToArray();
    }
}
