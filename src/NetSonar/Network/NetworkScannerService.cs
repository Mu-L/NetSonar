using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetSonar.Avalonia.Network;

/// <summary>
/// Routes a scan request to nmap when it is installed and wanted, and to the built-in engine otherwise.
/// </summary>
public static class NetworkScannerService
{
    /// <summary>
    /// Picks the engine used for the next scan.
    /// </summary>
    /// <param name="preferNmap">Whether nmap is used when it is installed.</param>
    /// <param name="executablePath">Receives the nmap path when the nmap engine is selected.</param>
    /// <returns>The selected engine.</returns>
    public static NetworkScanEngine ResolveEngine(bool preferNmap, out string executablePath)
    {
        if (preferNmap && NmapScannerService.TryFindExecutable(out executablePath))
        {
            return NetworkScanEngine.Nmap;
        }

        executablePath = string.Empty;
        return NetworkScanEngine.Builtin;
    }

    /// <summary>
    /// Runs host discovery with the selected engine.
    /// </summary>
    /// <param name="preferNmap">Whether nmap is used when it is installed.</param>
    /// <param name="target">The target expression.</param>
    /// <param name="options">The scan options.</param>
    /// <param name="progress">Receives scan progress.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    /// <returns>The discovered hosts.</returns>
    public static Task<NetworkScanResult> DiscoverAsync(
        bool preferNmap,
        string target,
        NetworkScanOptions options,
        IProgress<NetworkScanProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        return ResolveEngine(preferNmap, out var executablePath) == NetworkScanEngine.Nmap
            ? NmapScannerService.DiscoverAsync(
                executablePath,
                target,
                options,
                progress,
                cancellationToken
            )
            : BuiltinNetworkScanner.DiscoverAsync(target, options, progress, cancellationToken);
    }

    /// <summary>
    /// Runs a port scan with the selected engine.
    /// </summary>
    /// <param name="preferNmap">Whether nmap is used when it is installed.</param>
    /// <param name="hosts">The addresses to scan.</param>
    /// <param name="options">The port scan options.</param>
    /// <param name="progress">Receives scan progress.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    /// <returns>The scanned hosts.</returns>
    public static Task<NetworkScanResult> ScanPortsAsync(
        bool preferNmap,
        IEnumerable<string> hosts,
        NetworkPortScanOptions options,
        IProgress<NetworkScanProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        return ResolveEngine(preferNmap, out var executablePath) == NetworkScanEngine.Nmap
            ? NmapScannerService.ScanPortsAsync(
                executablePath,
                hosts,
                options,
                progress,
                cancellationToken
            )
            : BuiltinNetworkScanner.ScanPortsAsync(hosts, options, progress, cancellationToken);
    }
}
