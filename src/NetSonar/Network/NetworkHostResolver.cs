using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSonar.Avalonia.Network;

/// <summary>
/// Resolves the host name of a scanned address.
/// </summary>
public static class NetworkHostResolver
{
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Looks up the host name of an address.
    /// </summary>
    /// <param name="address">The address to resolve.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The host name, or an empty string when the address has none.</returns>
    /// <remarks>
    /// A reverse lookup of an unknown address can hang until the resolver gives up, so it is bounded by its own
    /// timeout on top of <paramref name="cancellationToken"/>.
    /// </remarks>
    public static async Task<string> ResolveHostNameAsync(
        string address,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(LookupTimeout);

            var entry = await Dns.GetHostEntryAsync(address, timeout.Token);
            return string.Equals(entry.HostName, address, StringComparison.Ordinal)
                ? string.Empty
                : entry.HostName;
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return string.Empty;
        }
        catch (SocketException)
        {
            return string.Empty;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}
