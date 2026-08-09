using NetSonar.Avalonia.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NetSonar.Avalonia.Models;

/// <summary>
/// Parses clipboard text copied from Excel (or any tab-separated / line-based source)
/// into a list of <see cref="NewPingService"/> entries.
/// Pure IP (no port) becomes an ICMP ping; IP:port or a separate numeric port column becomes a TCP probe.
/// Column layout: IP | port | interval(seconds) | description | group — every column after the
/// address is optional. If any row in a paste carries a numeric value right after its port slot,
/// that whole paste is read as including an interval column (blank interval cells keep the default).
/// </summary>
public static class ClipboardPasteParser
{
    private static readonly HashSet<string> HeaderTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "ip", "address", "host", "hostname", "url", "server", "target", "destination",
        "endpoint", "ipaddress", "name", "serveraddress", "地址", "服务器", "主机",
    };

    private static readonly HashSet<string> IntervalHeaderTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "interval", "pinginterval", "间隔", "间隔时间", "周期",
    };

    public static ClipboardPasteResult Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new([], 0);

        var rawRows = new List<string[]>();
        string[]? headerCells = null;
        var skipped = 0;
        var firstRow = true;

        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = line.Split('\t').Select(cell => cell.Trim()).ToArray();
            if (cells.Length == 0 || string.IsNullOrWhiteSpace(cells[0]))
            {
                skipped++;
                continue;
            }

            if (firstRow && IsHeaderRow(cells))
            {
                headerCells = cells;
                skipped++;
                firstRow = false;
                continue;
            }
            firstRow = false;

            // Address containing internal whitespace is not a usable target (e.g. "foo bar").
            if (cells[0].Any(char.IsWhiteSpace))
            {
                skipped++;
                continue;
            }

            // A clearly numeric second column that is out of port range is a malformed port row → skip.
            if (cells.Length >= 2
                && int.TryParse(cells[1], out var numericCell)
                && numericCell is < 1 or > 65535)
            {
                skipped++;
                continue;
            }

            rawRows.Add(cells);
        }

        // An interval column is present when any data row has a numeric value in the cell right after
        // its port slot, or when the header explicitly names such a column.
        var hasIntervalColumn = headerCells?.Any(c => IntervalHeaderTokens.Contains(c)) == true
            || rawRows.Any(row => TryParseInterval(IntervalCell(row)) is not null);

        var services = new List<NewPingService>();
        foreach (var cells in rawRows)
        {
            if (TryCreateService(cells, hasIntervalColumn, out var service) && service is not null) services.Add(service);
            else skipped++;
        }

        return new ClipboardPasteResult(services, skipped);
    }

    private static bool TryCreateService(string[] cells, bool hasIntervalColumn, out NewPingService? service)
    {
        service = null;
        var addr = cells[0];
        int port = 0;
        var hasSeparatePort = cells.Length >= 2 && TryParsePort(cells[1], out port);
        var addrHasPort = AddressHasPort(addr);
        var (description, group, interval) = ExtractTail(cells, hasSeparatePort, addrHasPort, hasIntervalColumn);

        ServiceProtocolType protocol;
        string protocolAddr;

        if (hasSeparatePort && !addrHasPort)
        {
            // Primary layout: "IP | port [| interval | description | group]" → append the port.
            protocol = ServiceProtocolType.TCP;
            protocolAddr = FormatAddressWithPort(addr, port);
        }
        else if (addrHasPort)
        {
            // Address already carries a port: "IP:port" / "host:port" / "[v6]:port".
            protocol = ServiceProtocolType.TCP;
            protocolAddr = addr;
        }
        else if (IPAddress.TryParse(addr, out _) || !addr.Contains(':'))
        {
            // Bare IP (IPv4 or IPv6) or hostname without a port → ICMP ping.
            protocol = ServiceProtocolType.ICMP;
            protocolAddr = addr.StartsWith('[') && addr.EndsWith(']') ? addr[1..^1] : addr;
        }
        else
        {
            // Colon but not a parseable endpoint (e.g. "host:http") → TCP; the validation gate rejects malformed ones.
            protocol = ServiceProtocolType.TCP;
            protocolAddr = addr;
        }

        var candidate = new NewPingService(protocol, protocolAddr, description, group);
        if (interval is double intervalSeconds) candidate.PingEverySeconds = intervalSeconds;
        if (!candidate.Validate()) return false;
        service = candidate;
        return true;
    }

    /// <summary>
    /// Detects whether <paramref name="addr"/> already encodes a port.
    /// A bracketed IPv6 literal ("[2001:db8::1]") or a bare IPv6 literal has no port.
    /// </summary>
    private static bool AddressHasPort(string addr)
    {
        if (addr.StartsWith('[')) return addr.Contains("]:", StringComparison.Ordinal);
        return addr.Contains(':') && !IPAddress.TryParse(addr, out _);
    }

    /// <summary>Index of the interval column for a given row: right after its port slot.</summary>
    private static int IntervalIndexFor(bool hasSeparatePort, bool addrHasPort)
        => hasSeparatePort && !addrHasPort ? 2 : addrHasPort ? 1 : 2;

    private static string? IntervalCell(string[] cells)
    {
        var addr = cells[0];
        var index = IntervalIndexFor(cells.Length >= 2 && TryParsePort(cells[1], out _), AddressHasPort(addr));
        return index < cells.Length ? cells[index] : null;
    }

    /// <summary>
    /// Maps the columns after the address to Interval (seconds), Description and Group.
    /// When the paste uses an interval column (<paramref name="hasIntervalColumn"/>), the interval slot
    /// is always consumed and blank cells keep the default; otherwise the classic
    /// "port | description | group" layout maps as before, with a blank port slot letting the
    /// description fall through so Excel rows with an empty port line up correctly.
    /// </summary>
    private static (string Description, string Group, double? IntervalSeconds) ExtractTail(
        string[] cells, bool hasSeparatePort, bool addrHasPort, bool hasIntervalColumn)
    {
        var intervalIndex = IntervalIndexFor(hasSeparatePort, addrHasPort);
        double? interval = null;
        if (hasIntervalColumn && intervalIndex < cells.Length)
            interval = TryParseInterval(cells[intervalIndex]);

        int tailStart;
        if (hasIntervalColumn)
            tailStart = intervalIndex + 1;
        else if (hasSeparatePort && !addrHasPort)
            tailStart = 2;
        else if (addrHasPort)
            tailStart = 1;
        else if (cells.Length >= 2 && !string.IsNullOrWhiteSpace(cells[1]))
            tailStart = 1;
        else
            tailStart = 2;

        var description = cells.Length > tailStart ? cells[tailStart] : string.Empty;
        var group = cells.Length > tailStart + 1 ? cells[tailStart + 1] : string.Empty;
        return (description, group, interval);
    }

    /// <summary>Parses a cell as a ping interval in seconds, within the app's valid range.</summary>
    private static double? TryParseInterval(string? cell)
    {
        return double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && value >= PingableService.MinPingEverySeconds && value <= PingableService.MaxPingEverySeconds
                ? value
                : null;
    }

    /// <summary>
    /// Heuristic for the first data row only: if the leading cell looks like a column header
    /// (English or Chinese address tokens), treat the whole row as a header and skip it.
    /// </summary>
    private static bool IsHeaderRow(string[] cells)
    {
        var first = cells[0].Trim().ToLowerInvariant();
        if (first.Length == 0 || !first.Any(char.IsLetter)) return false;
        return HeaderTokens.Contains(first) || first.StartsWith("ip", StringComparison.Ordinal);
    }

    private static bool TryParsePort(string? cell, out int port)
    {
        port = 0;
        return int.TryParse(cell, out port) && port is >= 1 and <= 65535;
    }

    private static string FormatAddressWithPort(string addr, int port)
    {
        if (addr.StartsWith('[')) return $"{addr}:{port}"; // already bracketed IPv6, e.g. "[2001:db8::1]"
        if (IPAddress.TryParse(addr, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return $"[{addr}]:{port}";
        }
        return $"{addr}:{port}";
    }
}

/// <summary>Result of parsing clipboard text into service entries.</summary>
/// <param name="Services">Valid services that were parsed.</param>
/// <param name="SkippedCount">Number of rows skipped (headers, blanks, invalid targets).</param>
public sealed record ClipboardPasteResult(IReadOnlyList<NewPingService> Services, int SkippedCount);
