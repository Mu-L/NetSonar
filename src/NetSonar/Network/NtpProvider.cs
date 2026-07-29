using System;
using System.Diagnostics.CodeAnalysis;

namespace NetSonar.Avalonia.Network;

public record NtpProvider : BaseProvider
{
    /// <summary>
    /// The NTP hostname (round-robin pools resolve to many IPs, so a hostname is used instead of a fixed address).
    /// </summary>
    public required string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// The NTP port. Defaults to the standard NTP port (123).
    /// </summary>
    public int Port { get; init; } = PingableService.DefaultNtpPort;

    public NtpProvider()
    {
    }

    [SetsRequiredMembers]
    public NtpProvider(string providerName, string hostname, string notes = "", int port = PingableService.DefaultNtpPort)
    {
        ProviderName = providerName;
        Hostname = hostname;
        Notes = notes;
        Port = port;
    }

    public static NtpProvider[] NtpProviders { get; } =
    [
        new("NTP Pool", "pool.ntp.org", "Global round-robin pool"),
        new("NTP Pool", "0.pool.ntp.org", "Global round-robin pool"),
        new("NTP Pool", "1.pool.ntp.org", "Global round-robin pool"),
        new("NTP Pool", "2.pool.ntp.org", "Global round-robin pool"),
        new("NTP Pool", "3.pool.ntp.org", "Global round-robin pool"),
        new("NTP Pool", "africa.pool.ntp.org", "Continental pool"),
        new("NTP Pool", "asia.pool.ntp.org", "Continental pool"),
        new("NTP Pool", "europe.pool.ntp.org", "Continental pool"),
        new("NTP Pool", "north-america.pool.ntp.org", "Continental pool"),
        new("NTP Pool", "oceania.pool.ntp.org", "Continental pool"),
        new("NTP Pool", "south-america.pool.ntp.org", "Continental pool"),

        new("Google", "time.google.com", "Leap-smeared"),
        new("Google", "time1.google.com", "Leap-smeared"),
        new("Google", "time2.google.com", "Leap-smeared"),
        new("Google", "time3.google.com", "Leap-smeared"),
        new("Google", "time4.google.com", "Leap-smeared"),

        new("Cloudflare", "time.cloudflare.com", "Supports NTS"),

        new("Apple", "time.apple.com"),
        new("Apple", "time1.apple.com"),
        new("Apple", "time2.apple.com"),
        new("Apple", "time3.apple.com"),
        new("Apple", "time4.apple.com"),
        new("Apple", "time5.apple.com"),
        new("Apple", "time6.apple.com"),
        new("Apple", "time7.apple.com"),

        new("Microsoft", "time.windows.com"),

        new("Meta", "time.facebook.com"),
        new("Meta", "time1.facebook.com"),
        new("Meta", "time2.facebook.com"),
        new("Meta", "time3.facebook.com"),
        new("Meta", "time4.facebook.com"),
        new("Meta", "time5.facebook.com"),

        new("Amazon", "time.aws.com", "Amazon Time Sync Service"),
        new("Amazon", "169.254.169.123", "Amazon Time Sync Service, link-local (EC2 only)"),

        new("VMware", "time.vmware.com"),

        new("Canonical", "ntp.ubuntu.com"),

        new("NIST", "time.nist.gov", "US National Institute of Standards and Technology"),
        new("NIST", "time-a-g.nist.gov", "Stratum 1, Gaithersburg MD"),
        new("NIST", "time-b-g.nist.gov", "Stratum 1, Gaithersburg MD"),
        new("NIST", "time-c-g.nist.gov", "Stratum 1, Gaithersburg MD"),
        new("NIST", "time-d-g.nist.gov", "Stratum 1, Gaithersburg MD"),

        new("NPL", "ntp1.npl.co.uk", "UK National Physical Laboratory, Stratum 1"),
        new("NPL", "ntp2.npl.co.uk", "UK National Physical Laboratory, Stratum 1"),

        new("PTB", "ptbtime1.ptb.de", "Germany Physikalisch-Technische Bundesanstalt, Stratum 1"),
        new("PTB", "ptbtime2.ptb.de", "Germany Physikalisch-Technische Bundesanstalt, Stratum 1"),

        new("NICT", "ntp.nict.jp", "Japan National Institute of Information and Communications Technology, Stratum 1"),

        new("VNIIFTRI", "ntp1.vniiftri.ru", "Russia, Stratum 1"),
        new("VNIIFTRI", "ntp2.vniiftri.ru", "Russia, Stratum 1"),
    ];
}