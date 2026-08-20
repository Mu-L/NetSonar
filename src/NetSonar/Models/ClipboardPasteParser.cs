using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Network;

namespace NetSonar.Avalonia.Models;

/// <summary>
/// Parses clipboard text copied from Excel (or any tab-separated / line-based source)
/// into a list of <see cref="NewPingService"/> entries.
/// Pure IP (no port) becomes an ICMP ping; known ports infer their service protocol and unknown ports use TCP.
/// A supported URI scheme explicitly selects the service protocol.
/// Column layout: IP | port | interval(seconds) | description | group — every column after the
/// address is optional. Rows may independently use the fixed layout or a compact layout.
/// </summary>
public static class ClipboardPasteParser
{
    private static readonly HashSet<string> HeaderTokens =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "ip", "address", "host", "hostname", "url", "server", "target", "destination",
        "endpoint", "ipaddress", "name", "serveraddress", "地址", "服务器", "主机"
    ];

    private static readonly HashSet<string> IntervalHeaderTokens =
    [
        with(StringComparer.OrdinalIgnoreCase),
        "interval", "pinginterval", "间隔", "间隔时间", "周期"
    ];

    public static ClipboardPasteResult Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new ClipboardPasteResult([], 0);

        var rawRows = new List<string[]>();
        string[]? headerCells = null;
        var skipped = 0;
        var firstRow = true;

        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cells = line.Split('\t', '|').Select(cell => cell.Trim()).ToArray();
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

            // A clearly numeric second column out of port range is a malformed port row when
            // the address does not already contain a port. Otherwise, it may be a valid interval.
            if (cells.Length >= 2
                && !AddressUsesCompactLayout(cells[0])
                && int.TryParse(cells[1], out var numericCell)
                && numericCell is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            {
                skipped++;
                continue;
            }

            rawRows.Add(cells);
        }

        var headerHasIntervalColumn = headerCells?.Any(c => IntervalHeaderTokens.Contains(c)) == true;

        var services = new List<NewPingService>();
        foreach (var cells in rawRows)
        {
            // Headerless pastes may mix compact rows with and without intervals. Do not let a numeric
            // interval in one row force descriptions in unrelated rows to be parsed as numbers.
            var rowHasIntervalColumn = headerHasIntervalColumn
                                       || HasExplicitIntervalSlot(cells)
                                       || TryParseInterval(IntervalCell(cells)) is not null;
            if (TryCreateService(cells, rowHasIntervalColumn, out var service) && service is not null)
                services.Add(service);
            else skipped++;
        }

        return new ClipboardPasteResult(services, skipped);
    }

    private static bool TryCreateService(string[] cells, bool hasIntervalColumn, out NewPingService? service)
    {
        service = null;
        var addr = cells[0];
        var hasExplicitProtocol = TryParseProtocolAddress(addr, out var protocol, out var protocolAddr);
        if (!hasExplicitProtocol && addr.Contains("://", StringComparison.Ordinal)) return false;

        var port = 0;
        var hasSeparatePortColumn = !hasExplicitProtocol
                                    && cells.Length >= 2
                                    && TryParsePort(cells[1], out port);
        var addrHasPort = AddressHasPort(addr);
        var usesCompactLayout = hasExplicitProtocol || addrHasPort;
        if (!TryExtractTail(cells, hasSeparatePortColumn, usesCompactLayout, hasIntervalColumn,
                out var description, out var group, out var interval))
        {
            return false;
        }

        if (!hasExplicitProtocol && hasSeparatePortColumn && port > IPEndPoint.MinPort && !addrHasPort)
        {
            protocol = GuessProtocol(port);
            protocolAddr = FormatAddressWithPort(addr, port);
        }
        else if (!hasExplicitProtocol && addrHasPort)
        {
            if (!TryGetAddressPort(addr, out var embeddedPort, out var host)) return false;

            protocol = GuessProtocol(embeddedPort);
            protocolAddr = embeddedPort == IPEndPoint.MinPort
                ? NormalizeIpLiteral(host)
                : addr;
        }
        else if (!hasExplicitProtocol
                 && (IPAddressExtensions.TryParseLiteral(addr, out var ipAddress) || !addr.Contains(':')))
        {
            // Bare IP (IPv4 or IPv6) or hostname without a port → ICMP ping.
            protocol = GuessProtocol(IPEndPoint.MinPort);
            protocolAddr = ipAddress?.ToString() ?? addr;
        }
        else if (!hasExplicitProtocol)
        {
            return false;
        }

        var candidate = new NewPingService(protocol, protocolAddr, description, group);
        if (interval is { } intervalSeconds) candidate.PingEverySeconds = intervalSeconds;
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
        if (IPAddressExtensions.TryParseLiteral(addr, out _)) return false;
        if (!addr.StartsWith('[')) return addr.Contains(':');

        var closingBracket = addr.IndexOf(']');
        return closingBracket >= 0
               && closingBracket + 1 < addr.Length
               && addr[closingBracket + 1] == ':';
    }

    private static bool AddressUsesCompactLayout(string addr)
    {
        return TryParseProtocolAddress(addr, out _, out _) || AddressHasPort(addr);
    }

    private static ServiceProtocolType GuessProtocol(int port)
    {
        return Protocols.ProtocolsByDefaultPort.GetValueOrDefault(port, ServiceProtocolType.TCP);
    }

    private static bool TryGetAddressPort(string address, out int port, out string host)
    {
        port = IPEndPoint.MinPort;
        host = address;
        int separatorIndex;
        if (address.StartsWith('['))
        {
            var closingBracket = address.IndexOf(']');
            separatorIndex = closingBracket >= 0 && closingBracket + 1 < address.Length
                                                 && address[closingBracket + 1] == ':'
                ? closingBracket + 1
                : -1;
        }
        else
        {
            separatorIndex = address.LastIndexOf(':');
        }

        if (separatorIndex < 0
            || !int.TryParse(address.AsSpan(separatorIndex + 1), out port)
            || port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            return false;
        }

        host = address[..separatorIndex];
        return true;
    }

    private static string NormalizeIpLiteral(string address)
    {
        return IPAddressExtensions.TryParseLiteral(address, out var ipAddress)
            ? ipAddress?.ToString() ?? address
            : address;
    }

    private static bool TryParseProtocolAddress(
        string address,
        out ServiceProtocolType protocol,
        out string protocolAddress)
    {
        protocol = default;
        protocolAddress = address;

        var separatorIndex = address.IndexOf("://", StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex + 3 >= address.Length) return false;

        var scheme = address.AsSpan(0, separatorIndex);
        var preserveScheme = false;
        if (scheme.Equals("icmp", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.ICMP;
        else if (scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.TCP;
        else if (scheme.Equals("udp", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.UDP;
        else if (scheme.Equals("tls", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.TLS;
        else if (scheme.Equals("dns", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.DNS;
        else if (scheme.Equals("ntp", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.NTP;
        else if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                 || scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            protocol = ServiceProtocolType.HTTP;
            preserveScheme = true;
        }
        else if (scheme.Equals("ws", StringComparison.OrdinalIgnoreCase)
                 || scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
        {
            protocol = ServiceProtocolType.WebSocket;
            preserveScheme = true;
        }
        else if (scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.SSH;
        else if (scheme.Equals("smtp", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.SMTP;
        else if (scheme.Equals("imap", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.IMAP;
        else if (scheme.Equals("mqtt", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.MQTT;
        else if (scheme.Equals("stun", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.STUN;
        else if (scheme.Equals("sip", StringComparison.OrdinalIgnoreCase))
            protocol = ServiceProtocolType.SIP;
        else
            return false;

        if (preserveScheme) return true;

        protocolAddress = address[(separatorIndex + 3)..];
        if (protocol == ServiceProtocolType.ICMP
            && IPAddressExtensions.TryParseLiteral(protocolAddress, out var ipAddress))
        {
            protocolAddress = ipAddress?.ToString() ?? protocolAddress;
        }

        return true;
    }

    /// <summary>Index of the interval column for a given row: right after its port slot.</summary>
    private static int IntervalIndexFor(string[] cells, bool addrHasPort)
    {
        // An embedded port can use the compact "host:port | interval" form. When column 2 is blank,
        // preserve the documented fixed layout: "host:port | [port] | interval".
        return addrHasPort && (cells.Length < 2 || !string.IsNullOrWhiteSpace(cells[1])) ? 1 : 2;
    }

    private static string? IntervalCell(string[] cells)
    {
        var addr = cells[0];
        var index = IntervalIndexFor(cells, AddressUsesCompactLayout(addr));
        return index < cells.Length ? cells[index] : null;
    }

    private static bool HasExplicitIntervalSlot(string[] cells)
    {
        // Five columns are the complete-documented layout. A blank second column also reserves the
        // port slot, so the following cell is unambiguously the interval even when it is malformed.
        return cells.Length >= 5
               || (cells.Length >= 3 && string.IsNullOrWhiteSpace(cells[1]));
    }

    /// <summary>
    /// Maps the columns after the address to Interval (seconds), Description, and Group.
    /// When the paste uses an interval column (<paramref name="hasIntervalColumn"/>), the interval slot
    /// is always consumed, and blank cells keep the default; otherwise the compact
    /// "port | description | group" layout maps as before.
    /// </summary>
    private static bool TryExtractTail(
        string[] cells,
        bool hasSeparatePortColumn,
        bool addrHasPort,
        bool hasIntervalColumn,
        out string description,
        out string group,
        out double? interval)
    {
        var intervalIndex = IntervalIndexFor(cells, addrHasPort);
        interval = null;
        if (hasIntervalColumn && intervalIndex < cells.Length)
        {
            var intervalCell = cells[intervalIndex];
            if (!string.IsNullOrWhiteSpace(intervalCell))
            {
                interval = TryParseInterval(intervalCell);
                if (interval is null)
                {
                    description = string.Empty;
                    group = string.Empty;
                    return false;
                }
            }
        }

        int tailStart;
        if (hasIntervalColumn)
            tailStart = intervalIndex + 1;
        else if (hasSeparatePortColumn && !addrHasPort)
            tailStart = 2;
        else if (addrHasPort)
            tailStart = 1;
        else if (cells.Length >= 2 && !string.IsNullOrWhiteSpace(cells[1]))
            tailStart = 1;
        else
            tailStart = 2;

        description = cells.Length > tailStart ? cells[tailStart] : string.Empty;
        group = cells.Length > tailStart + 1 ? cells[tailStart + 1] : string.Empty;
        return true;
    }

    /// <summary>Parses a cell as a ping interval in seconds, within the app's valid range.</summary>
    private static double? TryParseInterval(string? cell)
    {
        if (!ParseExtensions.TryParseLocalizedDouble(cell, out var value))
        {
            return null;
        }

        return value is >= PingableService.MinPingEverySeconds and <= PingableService.MaxPingEverySeconds
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
        return int.TryParse(cell, out port) && port is >= IPEndPoint.MinPort and <= IPEndPoint.MaxPort;
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
