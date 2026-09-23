using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using StageKit;

namespace NetSonar.Avalonia.Network;

/// <summary>
/// Resolves a MAC address to its vendor using the <c>nmap-mac-prefixes</c> database that ships with nmap.
/// </summary>
/// <remarks>
/// The database is only read when nmap is installed. Without it the lookup stays empty instead of shipping a
/// multi-megabyte copy of the IEEE registry inside the application.
/// </remarks>
public static class MacVendorLookup
{
    private const string DatabaseFileName = "nmap-mac-prefixes";

    private static readonly Lock LoadLock = new();
    private static Dictionary<string, string>? _prefixes;
    private static string? _loadedFrom;

    /// <summary>
    /// Gets the number of known prefixes, loading the database on first use.
    /// </summary>
    public static int Count => GetPrefixes().Count;

    /// <summary>
    /// Gets the file the database was loaded from, or <see langword="null"/> when no database was found.
    /// </summary>
    public static string? DatabasePath
    {
        get
        {
            GetPrefixes();
            return _loadedFrom;
        }
    }

    /// <summary>
    /// Resolves the vendor of a MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address, in any common separator form.</param>
    /// <returns>The vendor name, or an empty string when it is unknown.</returns>
    public static string Resolve(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress)) return string.Empty;

        var normalized = ArpTable.NormalizeMacAddress(macAddress);
        if (normalized.Length != 17) return string.Empty;

        Span<char> prefix = stackalloc char[6];
        prefix[0] = normalized[0];
        prefix[1] = normalized[1];
        prefix[2] = normalized[3];
        prefix[3] = normalized[4];
        prefix[4] = normalized[6];
        prefix[5] = normalized[7];

        return GetPrefixes().GetValueOrDefault(new string(prefix), string.Empty);
    }

    /// <summary>
    /// Discards the cached database so the next lookup searches for it again.
    /// </summary>
    public static void Invalidate()
    {
        lock (LoadLock)
        {
            _prefixes = null;
            _loadedFrom = null;
        }
    }

    private static Dictionary<string, string> GetPrefixes()
    {
        if (_prefixes is not null) return _prefixes;

        lock (LoadLock)
        {
            if (_prefixes is not null) return _prefixes;

            var prefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var path = FindDatabase();
            if (path is not null)
            {
                try
                {
                    foreach (var line in File.ReadLines(path))
                    {
                        var span = line.AsSpan().Trim();
                        if (span.IsEmpty || span[0] == '#') continue;

                        var separator = span.IndexOf(' ');
                        if (separator != 6) continue;

                        var vendor = span[(separator + 1)..].Trim();
                        if (vendor.IsEmpty) continue;

                        prefixes[new string(span[..separator])] = new string(vendor);
                    }

                    _loadedFrom = path;
                }
                catch (Exception e)
                {
                    UnhandledExceptions.HandleSafeException(e, nameof(MacVendorLookup));
                }
            }

            return _prefixes = prefixes;
        }
    }

    private static string? FindDatabase()
    {
        foreach (var directory in GetCandidateDirectories())
        {
            if (string.IsNullOrWhiteSpace(directory)) continue;

            var path = Path.Combine(directory, DatabaseFileName);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private static IEnumerable<string?> GetCandidateDirectories()
    {
        if (NmapScannerService.TryFindExecutable(out var executablePath))
        {
            var nmapDirectory = Path.GetDirectoryName(executablePath);
            yield return nmapDirectory;
            if (nmapDirectory is not null)
            {
                yield return Path.Combine(nmapDirectory, "..", "share", "nmap");
            }
        }

        yield return "/usr/share/nmap";
        yield return "/usr/local/share/nmap";
        yield return "/opt/homebrew/share/nmap";
        yield return "/opt/local/share/nmap";
    }
}
