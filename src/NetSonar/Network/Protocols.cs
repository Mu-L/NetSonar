using System.Collections.Generic;
using System.Net;

namespace NetSonar.Avalonia.Network;

public static class Protocols
{
    public const int DefaultSshPort = 22;
    public const int DefaultSmtpPort = 25;
    public const int DefaultDnsPort = 53;
    public const int DefaultHttpPort = 80;
    public const int DefaultNtpPort = 123;
    public const int DefaultImapPort = 143;
    public const int DefaultTlsPort = 443;
    public const int DefaultImapTlsPort = 993;
    public const int DefaultMqttPort = 1883;
    public const int DefaultStunPort = 3478;
    public const int DefaultSipPort = 5060;

    public static readonly IReadOnlyDictionary<int, ServiceProtocolType> ProtocolsByDefaultPort =
        new Dictionary<int, ServiceProtocolType>
        {
            [IPEndPoint.MinPort] = ServiceProtocolType.ICMP,
            [DefaultSshPort] = ServiceProtocolType.SSH,
            [DefaultSmtpPort] = ServiceProtocolType.SMTP,
            [DefaultDnsPort] = ServiceProtocolType.DNS,
            [DefaultHttpPort] = ServiceProtocolType.HTTP,
            [DefaultNtpPort] = ServiceProtocolType.NTP,
            [DefaultImapPort] = ServiceProtocolType.IMAP,
            [DefaultTlsPort] = ServiceProtocolType.TLS,
            [DefaultImapTlsPort] = ServiceProtocolType.IMAP,
            [DefaultMqttPort] = ServiceProtocolType.MQTT,
            [DefaultStunPort] = ServiceProtocolType.STUN,
            [DefaultSipPort] = ServiceProtocolType.SIP,

            [587] = ServiceProtocolType.SMTP,
            [8008] = ServiceProtocolType.HTTP,
            [8080] = ServiceProtocolType.HTTP,
            [19302] = ServiceProtocolType.STUN,

            // These application protocols require TLS on their secure ports, so use the generic TLS probe.
            [465] = ServiceProtocolType.TLS,
            [853] = ServiceProtocolType.TLS,
            [5061] = ServiceProtocolType.TLS,
            [5349] = ServiceProtocolType.TLS,
            [8443] = ServiceProtocolType.TLS,
            [8883] = ServiceProtocolType.TLS
        };
}
