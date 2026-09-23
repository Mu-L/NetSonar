using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NetSonar.Avalonia.Network;
using ObservableCollections;
using StageKit;

namespace NetSonar.Avalonia.Settings;

/// <summary>
/// The persisted configuration of the network scanner page.
/// </summary>
public partial class NetworkScannerSettings : SubSettings
{
    public const int MaxCustomTargets = 20;

    /// <summary>
    /// Gets or sets the target used by the last scan, restored on the next start.
    /// </summary>
    [ObservableProperty]
    public partial string LastTarget { get; set; } = string.Empty;

    /// <summary>
    /// Gets the targets the user typed manually, offered next to the detected local networks.
    /// </summary>
    public ObservableList<string> CustomTargets { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether nmap is used when it is installed.
    /// </summary>
    [ObservableProperty]
    public partial bool UseNmapWhenAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether host names are resolved.
    /// </summary>
    [ObservableProperty]
    public partial bool ResolveDns { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the scan runs with administrator privileges.
    /// </summary>
    /// <remarks>An elevated scan reports MAC addresses and vendors, and enables the UDP and OS options.</remarks>
    [ObservableProperty]
    public partial bool ElevatedScan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the service and version of each open port is probed.
    /// </summary>
    [ObservableProperty]
    public partial bool ServiceDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether UDP ports are scanned. Requires <see cref="ElevatedScan"/>.
    /// </summary>
    [ObservableProperty]
    public partial bool UdpScan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operating system is fingerprinted. Requires <see cref="ElevatedScan"/>.
    /// </summary>
    [ObservableProperty]
    public partial bool OsDetection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a toast reports the hosts and ports that changed between scans.
    /// </summary>
    [ObservableProperty]
    public partial bool NotifyChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the port range preset used by port scans.
    /// </summary>
    [ObservableProperty]
    public partial NetworkPortScanRange PortScanRange { get; set; } = NetworkPortScanRange.Top100;

    /// <summary>
    /// Gets or sets the port specification used when <see cref="PortScanRange"/> is
    /// <see cref="NetworkPortScanRange.Custom"/>.
    /// </summary>
    [ObservableProperty]
    public partial string CustomPorts { get; set; } = "22,53,80,139,443,445,3389,8080";

    /// <summary>
    /// Gets or sets the nmap timing template, from 0 (paranoid) to 5 (insane).
    /// </summary>
    public int TimingTemplate
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 0, 5));
    } = 4;

    /// <summary>
    /// Gets or sets the per-host time budget, in seconds. Zero disables the limit.
    /// </summary>
    public int HostTimeoutSeconds
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 0, 3600));
    } = 300;

    /// <summary>
    /// Gets or sets the per-probe timeout of the built-in engine, in milliseconds.
    /// </summary>
    public int ProbeTimeoutMilliseconds
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 100, 10_000));
    } = 1_000;

    /// <summary>
    /// Gets or sets the maximum number of addresses a single target may expand to.
    /// </summary>
    public int MaxHostsPerScan
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 1, 65_536));
    } = 4_096;

    /// <summary>
    /// Gets or sets the interval, in minutes, of the automatic rescan. Zero disables it.
    /// </summary>
    public int AutoRescanMinutes
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, 0, 1440));
    }

    /// <summary>
    /// Builds the discovery options from the current configuration.
    /// </summary>
    /// <returns>The scan options.</returns>
    public NetworkScanOptions ToScanOptions()
    {
        return new NetworkScanOptions
        {
            ResolveDns = ResolveDns,
            TimingTemplate = TimingTemplate,
            HostTimeoutSeconds = HostTimeoutSeconds,
            Elevated = ElevatedScan,
            ProbeTimeoutMilliseconds = ProbeTimeoutMilliseconds,
            MaxHosts = MaxHostsPerScan,
            ElevationPrompt = App.Localization["Navigation.NetworkScanner"],
        };
    }

    /// <summary>
    /// Builds the port scan options from the current configuration.
    /// </summary>
    /// <returns>The port scan options.</returns>
    public NetworkPortScanOptions ToPortScanOptions()
    {
        return new NetworkPortScanOptions
        {
            ResolveDns = ResolveDns,
            TimingTemplate = TimingTemplate,
            HostTimeoutSeconds = HostTimeoutSeconds,
            Elevated = ElevatedScan,
            ProbeTimeoutMilliseconds = ProbeTimeoutMilliseconds,
            MaxHosts = MaxHostsPerScan,
            ElevationPrompt = App.Localization["Navigation.NetworkScanner"],
            Range = PortScanRange,
            CustomPorts = CustomPorts,
            ServiceDetection = ServiceDetection,
            UdpScan = UdpScan,
            OsDetection = OsDetection,
        };
    }

    /// <summary>
    /// Remembers a manually typed target, keeping the list bounded and most-recent first.
    /// </summary>
    /// <param name="target">The target to remember.</param>
    /// <param name="knownTargets">The detected targets, which are not remembered.</param>
    public void RememberCustomTarget(string target, IEnumerable<string> knownTargets)
    {
        if (string.IsNullOrWhiteSpace(target)) return;

        var value = target.Trim();
        foreach (var known in knownTargets)
        {
            if (string.Equals(known, value, StringComparison.OrdinalIgnoreCase)) return;
        }

        for (var index = CustomTargets.Count - 1; index >= 0; index--)
        {
            if (string.Equals(CustomTargets[index], value, StringComparison.OrdinalIgnoreCase))
            {
                CustomTargets.RemoveAt(index);
            }
        }

        CustomTargets.Insert(0, value);
        while (CustomTargets.Count > MaxCustomTargets)
        {
            CustomTargets.RemoveAt(CustomTargets.Count - 1);
        }
    }
}
