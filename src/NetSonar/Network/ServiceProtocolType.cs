using System.ComponentModel;

namespace NetSonar.Avalonia.Network;

public enum ServiceProtocolType
{
    [Description("ICMP: Internet Control Message Protocol")]
    ICMP,

    [Description("UDP: User Datagram Protocol")]
    UDP,

    [Description("TCP: Transmission Control Protocol")]
    TCP,

    [Description("HTTP: Hypertext Transfer Protocol")]
    HTTP,

    [Description("NTP: Network Time Protocol")]
    NTP,
}
