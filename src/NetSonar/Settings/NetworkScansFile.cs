using System;
using System.Collections.Generic;
using System.Linq;
using NetSonar.Avalonia.Network;
using StageKit;

namespace NetSonar.Avalonia.Settings;

/// <summary>
/// One stored scan, used to restore the last result and to diff the next scan against it.
/// </summary>
public sealed class NetworkScanSnapshot
{
    public required string Target { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public NetworkScanEngine Engine { get; init; }
    public IReadOnlyList<NetworkScannerHost> Hosts { get; init; } = [];
}

/// <summary>
/// The persisted scan history, newest first.
/// </summary>
public sealed class NetworkScansFile : RootCollectionFile<NetworkScansFile, NetworkScanSnapshot>
{
    /// <summary>
    /// The number of scans kept on disk.
    /// </summary>
    public const int MaxSnapshots = 20;

    public NetworkScansFile()
    {
        AutoSave = true;
        DirectoryPath = ApplicationKit.ConfigsPath;
        FileName = "network_scans.json";
    }

    /// <summary>
    /// Returns the most recent stored scan of a target.
    /// </summary>
    /// <param name="target">The target to look up.</param>
    /// <returns>The snapshot, or <see langword="null"/> when the target was never scanned.</returns>
    public NetworkScanSnapshot? FindLatest(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        return this.FirstOrDefault(snapshot =>
            string.Equals(snapshot.Target, target.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Drops every stored scan of a target.
    /// </summary>
    /// <param name="target">The target to forget.</param>
    /// <returns><see langword="true"/> when a stored scan was removed.</returns>
    public bool RemoveTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)) return false;

        var value = target.Trim();
        var removed = false;

        for (var index = Count - 1; index >= 0; index--)
        {
            if (!string.Equals(this[index].Target, value, StringComparison.OrdinalIgnoreCase))
                continue;

            RemoveAt(index);
            removed = true;
        }

        return removed;
    }

    /// <summary>
    /// Stores a scan, replacing the previous scan of the same target and trimming the history.
    /// </summary>
    /// <param name="snapshot">The scan to store.</param>
    public void Store(NetworkScanSnapshot snapshot)
    {
        for (var index = Count - 1; index >= 0; index--)
        {
            if (
                string.Equals(this[index].Target, snapshot.Target, StringComparison.OrdinalIgnoreCase)
            )
            {
                RemoveAt(index);
            }
        }

        Insert(0, snapshot);
        while (Count > MaxSnapshots)
        {
            RemoveAt(Count - 1);
        }
    }
}
