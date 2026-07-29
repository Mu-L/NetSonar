using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic.FileIO;
using NetSonar.Avalonia.Models;

namespace NetSonar.Avalonia.Network;

public partial class PingableService : BasePingableCollectionObject<PingableServiceReply>
{
    #region Constants
    public const double MinPingEverySeconds = 0.50;
    public const double MaxPingEverySeconds = int.MaxValue;
    public const double DefaultPingEverySeconds = 5.0;

    public const double MinTimeoutSeconds = 0.1;
    public const double MaxTimeoutSeconds = ushort.MaxValue;
    public const double DefaultTimeoutSeconds = 5.0;

    public const byte DefaultTtl = 128;

    public const int MinBufferSize = 0;
    public const int MaxBufferSize = 65500;
    public const int DefaultBufferSize = 32;

    private const int WindowsIcmpTimeoutResolution = 500;

    public const int DefaultNtpPort = 123;
    private const int NtpPacketLength = 48;
    private const long NtpEpochOffsetSeconds = 2_208_988_800;

    #endregion

    #region Members

    private byte[]? _sendBuffer;
    private EndPoint? _socketEndPoint;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the buffer size of the ping.
    /// </summary>
    public int BufferSize
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, MinBufferSize, MaxBufferSize));
    } = 32;

    /// <summary>
    /// Gets or sets the maximum time to live (TTL),
    /// the amount of time or "hops" that a packet is set to exist inside
    /// a network before being discarded by a router.
    /// </summary>
    public byte Ttl
    {
        get;
        set => SetProperty(ref field, Math.Max((byte)1, value));
    } = DefaultTtl;


    /// <summary>
    /// Gets or sets if the ping packet can be fragmented.
    /// </summary>
    [ObservableProperty]
    public partial bool DontFragment { get; set; }

    [JsonIgnore]
    public bool CanUseBufferSize => ProtocolType is ServiceProtocolType.ICMP or ServiceProtocolType.TCP or ServiceProtocolType.UDP;

    [JsonIgnore]
    public bool CanUseTtl => ProtocolType is ServiceProtocolType.ICMP or ServiceProtocolType.TCP or ServiceProtocolType.UDP or ServiceProtocolType.NTP;

    [JsonIgnore]
    public bool CanUseDontFragment => ProtocolType is ServiceProtocolType.ICMP;

    /// <summary>
    /// Gets if the default ping options are used.
    /// </summary>
    [JsonIgnore]
    public bool UseDefaultPingOptions => Ttl is 0 or DefaultTtl && !DontFragment;

    /// <summary>
    /// Gets the ping options.
    /// </summary>
    [JsonIgnore]
    public PingOptions PingOptions
    {
        get
        {
            var options = field;
            if (options is null || options.Ttl != Ttl || options.DontFragment != DontFragment)
            {
                options = new PingOptions(Ttl, DontFragment);
                field = options;
            }

            return options;
        }
    }
    #endregion

    #region Constructor

    [SetsRequiredMembers]
    public PingableService(ServiceProtocolType protocolType, string ipAddressOrUrl) : base(protocolType, ipAddressOrUrl)
    {
    }

    [SetsRequiredMembers]
    [JsonConstructor]
    public PingableService(ServiceProtocolType protocolType, string ipAddressOrUrl, string description = "", string group = "") : base(protocolType, ipAddressOrUrl, description, group)
    {
    }

    [SetsRequiredMembers]
    public PingableService(NewPingService service) : base(service.ProtocolType, service.IpAddressOrUrl, service.Description, service.Group)
    {
        IsEnabled = service.IsEnabled;
        PingEverySeconds = service.PingEverySeconds;
        TimeoutSeconds = service.TimeoutSeconds;
        BufferSize = service.BufferSize;
        Ttl = service.Ttl;
        DontFragment = service.DontFragment;
    }

    #endregion

    #region Methods

    [MemberNotNull(nameof(_sendBuffer))]
    private void EnsureBuffer()
    {
        if (_sendBuffer is null || _sendBuffer.Length != BufferSize)
        {
            _sendBuffer = CreateBuffer(BufferSize);
        }
    }

    private static int GetEffectiveIcmpTimeout(int timeout)
    {
        if (!OperatingSystem.IsWindows() || timeout <= 0) return timeout;

        // Windows floors ICMP timeouts to 500 ms; round up so the effective deadline is never shorter.
        var remainder = timeout % WindowsIcmpTimeoutResolution;
        if (remainder == 0) return timeout;

        var adjustment = WindowsIcmpTimeoutResolution - remainder;
        return timeout > int.MaxValue - adjustment ? int.MaxValue : timeout + adjustment;
    }

    private EndPoint GetSocketEndPoint()
    {
        if (_socketEndPoint is not null) return _socketEndPoint;
        if (IPEndPoint.TryParse(IpAddressOrUrl, out var ipEndPoint))
        {
            if (ProtocolType == ServiceProtocolType.NTP && ipEndPoint.Port <= IPEndPoint.MinPort)
            {
                ipEndPoint.Port = DefaultNtpPort;
            }

            return _socketEndPoint = ipEndPoint;
        }

        var scheme = ProtocolType switch
        {
            ServiceProtocolType.TCP => "tcp",
            ServiceProtocolType.UDP => "udp",
            ServiceProtocolType.NTP => "udp",
            _ => throw new InvalidOperationException(
                $"{nameof(GetSocketEndPoint)} does not support the {ProtocolType} protocol.")
        };
        if (!Uri.TryCreate($"{scheme}://{IpAddressOrUrl}", UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.IdnHost))
        {
            throw new ArgumentException($"Invalid {ProtocolType} host and port ({IpAddressOrUrl}).", nameof(IpAddressOrUrl));
        }

        var port = uri.Port;
        if (port <= IPEndPoint.MinPort)
        {
            if (ProtocolType != ServiceProtocolType.NTP)
            {
                throw new ArgumentException($"Invalid {ProtocolType} host and port ({IpAddressOrUrl}).", nameof(IpAddressOrUrl));
            }

            port = DefaultNtpPort;
        }

        return _socketEndPoint = new DnsEndPoint(uri.IdnHost, port);
    }

    private static async ValueTask<int> ReceiveUdpResponseAsync(Socket socket, CancellationToken cancellationToken)
    {
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(MaxBufferSize);
        try
        {
            return await socket.ReceiveAsync(
                receiveBuffer.AsMemory(0, MaxBufferSize),
                SocketFlags.None,
                cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }
    }

    private static void CreateNtpRequest(Span<byte> packet)
    {
        packet[..NtpPacketLength].Clear();
        packet[0] = 0x23; // LI = 0, version = 4, mode = 3 (client).

        var elapsedTicks = DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;
        var seconds = elapsedTicks / TimeSpan.TicksPerSecond + NtpEpochOffsetSeconds;
        var fractionalTicks = elapsedTicks % TimeSpan.TicksPerSecond;
        var fraction = (uint)(fractionalTicks * 0x1_0000_0000L / TimeSpan.TicksPerSecond);

        BinaryPrimitives.WriteUInt32BigEndian(packet[40..44], unchecked((uint)seconds));
        BinaryPrimitives.WriteUInt32BigEndian(packet[44..48], fraction);
    }

    private static async ValueTask<int> ReceiveAndValidateNtpResponseAsync(
        Socket socket,
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken)
    {
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(MaxBufferSize);
        try
        {
            var length = await socket.ReceiveAsync(
                receiveBuffer.AsMemory(0, MaxBufferSize),
                SocketFlags.None,
                cancellationToken);
            ValidateNtpResponse(receiveBuffer.AsSpan(0, length), request.Span);
            return length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }
    }

    private static void ValidateNtpResponse(ReadOnlySpan<byte> response, ReadOnlySpan<byte> request)
    {
        if (response.Length < NtpPacketLength)
        {
            throw new InvalidDataException(
                $"The NTP response is {response.Length} bytes; at least {NtpPacketLength} bytes are required.");
        }

        var leapIndicator = response[0] >> 6;
        var version = response[0] >> 3 & 0x07;
        var mode = response[0] & 0x07;
        var stratum = response[1];

        if (mode != 4)
        {
            throw new InvalidDataException($"The NTP response has mode {mode}; server mode 4 is required.");
        }

        if (version is < 3 or > 4)
        {
            throw new InvalidDataException($"The NTP response has unsupported version {version}.");
        }

        if (leapIndicator == 3)
        {
            throw new InvalidDataException("The NTP server reports that its clock is not synchronized.");
        }

        if (stratum is 0 or > 15)
        {
            throw new InvalidDataException($"The NTP response has invalid or unsynchronized stratum {stratum}.");
        }

        if (!response.Slice(24, 8).SequenceEqual(request.Slice(40, 8)))
        {
            throw new InvalidDataException("The NTP response does not match the request transmit timestamp.");
        }

        if (IsZeroTimestamp(response.Slice(32, 8)) || IsZeroTimestamp(response.Slice(40, 8)))
        {
            throw new InvalidDataException("The NTP response has an empty receive or transmit timestamp.");
        }
    }

    private static bool IsZeroTimestamp(ReadOnlySpan<byte> timestamp)
    {
        foreach (var value in timestamp)
        {
            if (value != 0) return false;
        }

        return true;
    }

    private static CancellationTokenSource CreateTimeoutTokenSource(int timeout, CancellationToken cancellationToken)
    {
        var timeoutCts = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : new CancellationTokenSource();
        if (timeout > 0) timeoutCts.CancelAfter(timeout);
        return timeoutCts;
    }

    private static double GetElapsedMilliseconds(long startingTimestamp)
    {
        return Math.Round(
            Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds,
            2,
            MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    protected override PingableServiceReply PingCore(int timeout = 0)
    {

        if (ProtocolType == ServiceProtocolType.ICMP)
        {
            EnsureBuffer();
            var effectiveTimeout = GetEffectiveIcmpTimeout(timeout);

            using var ping = new Ping();
            var sentDateTime = DateTime.Now;
            try
            {
                PingReply reply;
                if (OperatingSystem.IsLinux() && !Environment.IsPrivilegedProcess)
                {
                    reply = ping.Send(IpAddressOrUrl, effectiveTimeout);
                }
                else
                {
                    reply = ping.Send(IpAddressOrUrl, effectiveTimeout, _sendBuffer, UseDefaultPingOptions ? null : PingOptions);
                }

                return new PingableServiceReply(reply);
            }
            catch (Exception e)
            {
                return PingableServiceReply.CreateErrorReply(e.InnerException?.Message ?? e.Message, sentDateTime);
            }
        }
        else if (ProtocolType is ServiceProtocolType.TCP or ServiceProtocolType.UDP or ServiceProtocolType.NTP)
        {
            return PingCoreAsync(timeout).Result;
        }
        else if (ProtocolType == ServiceProtocolType.HTTP)
        {
            return PingCoreAsync(timeout).Result;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(ProtocolType), ProtocolType, null);
        }
    }


    /// <inheritdoc />
    protected override async Task<PingableServiceReply> PingCoreAsync(int timeout = 0, CancellationToken cancellationToken = default)
    {
        if (ProtocolType == ServiceProtocolType.ICMP)
        {
            EnsureBuffer();
            var effectiveTimeout = GetEffectiveIcmpTimeout(timeout);

            using var ping = new Ping();
            var sentOn = DateTime.Now;
            try
            {
                PingReply reply;
                if (OperatingSystem.IsLinux() && !Environment.IsPrivilegedProcess)
                {
                    reply = await ping.SendPingAsync(IpAddressOrUrl, TimeSpan.FromMilliseconds(effectiveTimeout), cancellationToken:cancellationToken);
                }
                else
                {
                    reply = await ping.SendPingAsync(IpAddressOrUrl, TimeSpan.FromMilliseconds(effectiveTimeout), _sendBuffer, UseDefaultPingOptions ? null : PingOptions, cancellationToken);
                }

                return new PingableServiceReply(reply);

            }
            catch (OperationCanceledException e)
            {
                return PingableServiceReply.CreateTimeOutReply(e, sentOn);
            }
            catch (Exception e)
            {
                return PingableServiceReply.CreateErrorReply(e, sentOn);
            }
        }
        else if (ProtocolType is ServiceProtocolType.TCP or ServiceProtocolType.UDP or ServiceProtocolType.NTP)
        {
            byte[]? ntpRequestBuffer = null;
            ReadOnlyMemory<byte> sendBuffer;
            if (ProtocolType == ServiceProtocolType.NTP)
            {
                ntpRequestBuffer = ArrayPool<byte>.Shared.Rent(NtpPacketLength);
                CreateNtpRequest(ntpRequestBuffer);
                sendBuffer = ntpRequestBuffer.AsMemory(0, NtpPacketLength);
            }
            else
            {
                EnsureBuffer();
                sendBuffer = _sendBuffer;
            }

            var startingTimestamp = Stopwatch.GetTimestamp();
            var sentDateTime = DateTime.Now;

            try
            {
                var remoteEndPoint = GetSocketEndPoint();
                var socketType = ProtocolType == ServiceProtocolType.TCP ? SocketType.Stream : SocketType.Dgram;
                var socketProtocol = ProtocolType == ServiceProtocolType.TCP ? System.Net.Sockets.ProtocolType.Tcp : System.Net.Sockets.ProtocolType.Udp;
                using var socket = remoteEndPoint is IPEndPoint endpoint
                    ? new Socket(endpoint.AddressFamily, socketType, socketProtocol)
                    : new Socket(socketType, socketProtocol);
                socket.Ttl = Ttl;

                using var timeoutCts = CreateTimeoutTokenSource(timeout, cancellationToken);
                await socket.ConnectAsync(remoteEndPoint, timeoutCts.Token);

                int bufferLength;
                if (ProtocolType == ServiceProtocolType.UDP)
                {
                    await socket.SendAsync(sendBuffer, timeoutCts.Token);
                    // UDP has no connection handshake; only a response verifies the remote service.
                    bufferLength = await ReceiveUdpResponseAsync(socket, timeoutCts.Token);
                }
                else if (ProtocolType == ServiceProtocolType.NTP)
                {
                    await socket.SendAsync(sendBuffer, timeoutCts.Token);
                    bufferLength = await ReceiveAndValidateNtpResponseAsync(socket, sendBuffer, timeoutCts.Token);
                }
                else
                {
                    if (sendBuffer.Length > 0)
                    {
                        await socket.SendAsync(sendBuffer, timeoutCts.Token);
                    }

                    bufferLength = sendBuffer.Length;
                }

                var connectedEndPoint = socket.RemoteEndPoint as IPEndPoint ?? IpEndPoint;
                return new PingableServiceReply(true, IPStatus.Success, connectedEndPoint, sentDateTime, GetElapsedMilliseconds(startingTimestamp), bufferLength, Ttl);
            }
            catch (OperationCanceledException e)
            {
                return PingableServiceReply.CreateTimeOutReply(e, sentDateTime);
            }
            catch (SocketException e)
            {
                return PingableServiceReply.CreateErrorReply(e.SocketErrorCode, e, sentDateTime);
            }
            catch (Exception e)
            {
                return PingableServiceReply.CreateErrorReply(e, sentDateTime);
            }
            finally
            {
                if (ntpRequestBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(ntpRequestBuffer);
                }
            }
        }
        else if (ProtocolType == ServiceProtocolType.HTTP)
        {
            var sentDateTime = DateTime.Now;

            try
            {
                using var timeoutCts = CreateTimeoutTokenSource(timeout, cancellationToken);

                var startingTimestamp = Stopwatch.GetTimestamp();
                using var request = new HttpRequestMessage(HttpMethod.Get, IpAddressOrUrl);
                using var response = await App.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
                var contentLength = Math.Min(response.Content.Headers.ContentLength.GetValueOrDefault(), int.MaxValue);
                var replyEndPoint = new IPEndPoint(IpEndPoint.Address, IpEndPoint.Port);
                return new PingableServiceReply(response.IsSuccessStatusCode, response.StatusCode, replyEndPoint, sentDateTime, GetElapsedMilliseconds(startingTimestamp), (int)contentLength);
            }
            catch (OperationCanceledException e)
            {
                return PingableServiceReply.CreateTimeOutReply(e, sentDateTime);
            }
            catch (HttpRequestException e)
            {
                return PingableServiceReply.CreateErrorReply(e.HttpRequestError, e, sentDateTime);
            }
            catch (Exception e)
            {
                return PingableServiceReply.CreateErrorReply(e, sentDateTime);
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(ProtocolType), ProtocolType, null);
        }
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Creates a buffer with the specified size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static byte[] CreateBuffer(int size = 32)
    {
        var buffer = new byte[size];
        for (var i = 0; i < size; i++)
        {
            buffer[i] = (byte)('a' + i % 23);
        }

        return buffer;
    }

    /// <summary>
    /// Tries to parse a string to a <see cref="PingableService"/>.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="service"></param>
    /// <returns></returns>
    public static bool TryParseFromString(string line, [NotNullWhen(true)] out PingableService? service)
    {
        try
        {
            service = ParseFromString(line);
            return true;
        }
        catch(Exception)
        {
            service = null;
            return false;
        }
    }

    public static PingableService ParseFromString(string line)
    {
        line = line.Trim();
        var pingSettings = line.Split('|', StringSplitOptions.TrimEntries);
        var serviceFields = pingSettings[0].Split(',', StringSplitOptions.TrimEntries);
        if (serviceFields.Length == 0 || string.IsNullOrWhiteSpace(serviceFields[0]))
        {
            throw new MalformedLineException("The service address must not be empty.");
        }

        var ipAddressOrUrl = serviceFields[0];
        var description = serviceFields.Length >= 2 ? serviceFields[1] : string.Empty;
        var group = serviceFields.Length >= 3 ? serviceFields[2] : string.Empty;

        ServiceProtocolType protocol;
        if (ipAddressOrUrl.Contains('/'))
        {

            if (ipAddressOrUrl.StartsWith("icmp://", StringComparison.OrdinalIgnoreCase))
            {
                ipAddressOrUrl = ipAddressOrUrl.Remove(0, "icmp://".Length);
                protocol = ServiceProtocolType.ICMP;
                if (ipAddressOrUrl.Contains(':')) throw new MalformedLineException($"The {protocol} protocol must not contain a port number.");
                if (ipAddressOrUrl.Contains('/')) throw new MalformedLineException($"The address must not contain path separator '/' for the {protocol} protocol.");
            }
            else if (ipAddressOrUrl.StartsWith("tcp://"))
            {
                ipAddressOrUrl = ipAddressOrUrl.Remove(0, "tcp://".Length);
                protocol = ServiceProtocolType.TCP;
                if (!ipAddressOrUrl.Contains(':')) throw new MalformedLineException($"The {protocol} protocol must contain a port number.");
                if (ipAddressOrUrl.Contains('/')) throw new MalformedLineException($"The address must not contain path separator '/' for the {protocol} protocol.");
            }
            else if (ipAddressOrUrl.StartsWith("udp://", StringComparison.OrdinalIgnoreCase))
            {
                ipAddressOrUrl = ipAddressOrUrl.Remove(0, "udp://".Length);
                protocol = ServiceProtocolType.UDP;
                if (!ipAddressOrUrl.Contains(':')) throw new MalformedLineException($"The {protocol} protocol must contain a port number.");
                if (ipAddressOrUrl.Contains('/')) throw new MalformedLineException($"The address must not contain path separator '/' for the {protocol} protocol.");
            }
            else if (ipAddressOrUrl.StartsWith("ntp://", StringComparison.OrdinalIgnoreCase))
            {
                ipAddressOrUrl = ipAddressOrUrl.Remove(0, "ntp://".Length);
                protocol = ServiceProtocolType.NTP;
                if (ipAddressOrUrl.Contains('/')) throw new MalformedLineException($"The address must not contain path separator '/' for the {protocol} protocol.");
            }
            else if (ipAddressOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || ipAddressOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                protocol = ServiceProtocolType.HTTP;
            }
            else
            {
                protocol = ServiceProtocolType.HTTP;
            }
        }
        else
        {
            protocol = ServiceProtocolType.ICMP;
            if (ipAddressOrUrl.Contains(':')) throw new MalformedLineException($"The {protocol} protocol must not contain a port number.");
        }

        double pingEvery = pingSettings.Length >= 2
            ? double.TryParse(pingSettings[1], CultureInfo.InvariantCulture, out var pingEveryResult) ? pingEveryResult : App.AppSettings.PingServices.DefaultPingEverySeconds
            : App.AppSettings.PingServices.DefaultPingEverySeconds;

        double timeout = pingSettings.Length >= 3
            ? double.TryParse(pingSettings[2], CultureInfo.InvariantCulture,  out var timeoutResult) ? timeoutResult : App.AppSettings.PingServices.DefaultTimeoutSeconds
            : App.AppSettings.PingServices.DefaultTimeoutSeconds;

        int bufferSize = pingSettings.Length >= 4
            ? int.TryParse(pingSettings[3], out var bufferResult) ? bufferResult : App.AppSettings.PingServices.DefaultBufferSize
            : App.AppSettings.PingServices.DefaultBufferSize;

        return new PingableService(protocol, ipAddressOrUrl, description, group)
        {
            Group = group,
            Description = description,
            PingEverySeconds = pingEvery,
            TimeoutSeconds = timeout,
            BufferSize = bufferSize
        };


    }
    public static List<PingableService> ParseFromText(string text)
    {
        var result = new List<PingableService>();
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (TryParseFromString(line, out var service)) result.Add(service);
        }
        return result;
    }

    #endregion
}
