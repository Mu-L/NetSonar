using System.ComponentModel;

namespace NetSonar.Avalonia.Network;

public enum ServiceProtocolType
{
    [Description("ICMP: Internet Control Message Protocol")]
    ICMP,

    [Description("TCP: Transmission Control Protocol")]
    TCP,

    [Description("UDP: User Datagram Protocol")]
    UDP,

    [Description("TLS: Transport Layer Security")]
    TLS,

    [Description("DNS: Domain Name System")]
    DNS,

    [Description("NTP: Network Time Protocol")]
    NTP,

    [Description("HTTP: Hypertext Transfer Protocol")]
    HTTP,

    [Description("WebSocket: Full-duplex communication protocol")]
    WebSocket,

    [Description("SSH: Secure Shell Protocol")]
    SSH,

    [Description("SMTP: Simple Mail Transfer Protocol")]
    SMTP,

    [Description("IMAP: Internet Message Access Protocol")]
    IMAP,

    [Description("MQTT: Message Queuing Telemetry Transport")]
    MQTT,

    [Description("STUN: Session Traversal Utilities for NAT")]
    STUN,

    [Description("SIP: Session Initiation Protocol")]
    SIP,
}
