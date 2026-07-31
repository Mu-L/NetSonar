using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Material.Icons;
using NetSonar.Avalonia.Extensions;
using ZLinq;

namespace NetSonar.Avalonia.Network;

public record DnsProvider : BaseProvider
{
    /// <summary>
    /// The block categories of the DNS provider.
    /// </summary>
    public DnsBlockCategories BlockCategories { get; init; } = DnsBlockCategories.Unfiltered;

    public string DnsBlockCategoriesString
    {
        get
        {
            if (BlockCategories
                is DnsBlockCategories.Unfiltered
                or DnsBlockCategories.Uncensored) return BlockCategories.ToString();
            var flags = BlockCategories.GetSetFlags().AsValueEnumerable().Skip(1);

            return $"Blocks {flags.JoinToString(", ")}";
        }
    }

    /// <summary>
    /// The primary IPv4 DNS address.
    /// </summary>
    public required IPAddress DNSv4PrimaryAddress { get; init; } = IPAddress.Any;

    /// <summary>
    /// The secondary IPv4 DNS address.
    /// </summary>
    public required IPAddress DNSv4SecondaryAddress { get; init; } = IPAddress.Any;

    /// <summary>
    /// The primary IPv6 DNS address.
    /// </summary>
    public required IPAddress DNSv6PrimaryAddress { get; init; } = IPAddress.IPv6Any;

    /// <summary>
    /// The secondary IPv6 DNS address.
    /// </summary>
    public required IPAddress DNSv6SecondaryAddress { get; init; } = IPAddress.IPv6Any;

    /// <summary>
    /// The https DNS address.
    /// </summary>
    public string HttpsAddress => string.IsNullOrEmpty(Hostname) ? string.Empty : $"https://{Hostname}/dns-query";

    /// <summary>
    /// The tls DNS address.
    /// </summary>
    public string TlsAddress => string.IsNullOrEmpty(Hostname) ? string.Empty : $"tls://{Hostname}";

    /// <summary>
    /// The quic DNS address.
    /// </summary>
    public string QuicAddress => string.IsNullOrEmpty(Hostname) ? string.Empty : $"quic://{Hostname}";

    public override string FormatedDescription => $"{ProviderName} ({string.Join(", ", BlockCategories)})";

    [SetsRequiredMembers]
    public DnsProvider(string providerName, DnsBlockCategories blockCategories,
        IPAddress dnsv4PrimaryAddress, IPAddress dnsv4SecondaryAddress,
        IPAddress dnsv6PrimaryAddress, IPAddress dnsv6SecondaryAddress,
        string hostname = "")
    {
        ProtocolType = ServiceProtocolType.DNS;
        ProviderName = providerName;
        BlockCategories = blockCategories;
        DNSv4PrimaryAddress = dnsv4PrimaryAddress;
        DNSv4SecondaryAddress = dnsv4SecondaryAddress;
        DNSv6PrimaryAddress = dnsv6PrimaryAddress;
        DNSv6SecondaryAddress = dnsv6SecondaryAddress;
        Hostname = hostname;
    }

    [SetsRequiredMembers]
    public DnsProvider(string providerName, DnsBlockCategories blockCategories,
        string dnsv4PrimaryAddress, string dnsv4SecondaryAddress,
        string dnsv6PrimaryAddress, string dnsv6SecondaryAddress,
        string hostname = "") : this(providerName, blockCategories,
        IPAddress.Parse(dnsv4PrimaryAddress), IPAddress.Parse(dnsv4SecondaryAddress),
        IPAddress.Parse(dnsv6PrimaryAddress), IPAddress.Parse(dnsv6SecondaryAddress),
        hostname)
    {
    }

    [SetsRequiredMembers]
    public DnsProvider(string providerName,
        string dnsv4PrimaryAddress, string dnsv4SecondaryAddress,
        string dnsv6PrimaryAddress, string dnsv6SecondaryAddress,
        string hostname = "") : this(providerName, DnsBlockCategories.Unfiltered,
        IPAddress.Parse(dnsv4PrimaryAddress), IPAddress.Parse(dnsv4SecondaryAddress),
        IPAddress.Parse(dnsv6PrimaryAddress), IPAddress.Parse(dnsv6SecondaryAddress),
        hostname)
    {
    }

    public static Dictionary<DnsBlockCategories, MaterialIconKind> DnsBlockCategoryIconKind { get; } = new()
    {
        { DnsBlockCategories.Malware, MaterialIconKind.ShieldBugOutline },
        { DnsBlockCategories.Phishing, MaterialIconKind.HookOff },
        { DnsBlockCategories.Ads, MaterialIconKind.AdsOff },
        { DnsBlockCategories.Tracking, MaterialIconKind.MapMarkerOffOutline },
        { DnsBlockCategories.SocialMedia, MaterialIconKind.ChatRemoveOutline },
        { DnsBlockCategories.Adult, MaterialIconKind.HumanMaleFemaleChild },
        { DnsBlockCategories.Drugs, MaterialIconKind.NeedleOff },
        { DnsBlockCategories.Gambling, MaterialIconKind.PokerChip },
        { DnsBlockCategories.FakeNews, MaterialIconKind.NewspaperRemove },
        { DnsBlockCategories.Cryptocurrency, MaterialIconKind.Bitcoin },
        { DnsBlockCategories.Proxies, MaterialIconKind.ArrowDecision },
        { DnsBlockCategories.VPN, MaterialIconKind.Vpn },
    };

    public static DnsProvider[] DnsProviders { get; } =
    [
        new("AdGuard", "94.140.14.140", "94.140.14.141", "2a10:50c0::1:ff", "2a10:50c0::2:ff", "unfiltered.adguard-dns.com")
        {
            BlockCategories = DnsBlockCategories.Unfiltered,
        },
        new("AdGuard", "94.140.14.14", "94.140.15.15", "2a10:50c0::ad1:ff", "2a10:50c0::ad2:ff", "dns.adguard-dns.com")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking,
        },
        new("AdGuard", "94.140.14.15", "94.140.15.16", "2a10:50c0::bad1:ff", "2a10:50c0::bad2:ff", "family.adguard-dns.com")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.Adult
        },

        new("Ali", "223.5.5.5", "223.6.6.6", "2400:3200::1", "2400:3200:baba::1", "dns.alidns.com")
        {
            BlockCategories = DnsBlockCategories.Unfiltered
        },

        new("AlternateDNS", "76.76.19.19", "76.223.122.150", "2602:fcbc::ad", "2602:fcbc:2::ad")
        {
            BlockCategories = DnsBlockCategories.Ads
        },

        new("ControlD", "76.76.2.0", "76.76.10.0", "2606:1a40::", "2606:1a40:1::")
        {
            BlockCategories = DnsBlockCategories.Unfiltered
        },
        new("ControlD", "76.76.2.1", "76.76.10.1", "2606:1a40::1", "2606:1a40:1::1")
        {
            BlockCategories = DnsBlockCategories.Malware
        },
        new("ControlD", "76.76.2.2", "76.76.10.2", "2606:1a40::2", "2606:1a40:1::2")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking
        },
        new("ControlD", "76.76.2.3", "76.76.10.3", "2606:1a40::3", "2606:1a40:1::3")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.SocialMedia
        },
        new("ControlD", "76.76.2.4", "76.76.10.4", "2606:1a40::4", "2606:1a40:1::4")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.SocialMedia | DnsBlockCategories.Adult | DnsBlockCategories.Drugs
        },
        new("ControlD", "76.76.2.5", "76.76.10.5", "2606:1a40::5", "2606:1a40:1::5")
        {
            BlockCategories = DnsBlockCategories.Uncensored
        },

        new("CleanBrowsing", "185.228.168.9", "185.228.169.9", "2a0d:2a00:1::2", "2a0d:2a00:2::2", "security-filter-dns.cleanbrowsing.org")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing
        },
        new("CleanBrowsing", "185.228.168.10", "185.228.169.11", "2a0d:2a00:1::1", "2a0d:2a00:2::1", "adult-filter-dns.cleanbrowsing.org")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Adult,
        },
        new("CleanBrowsing", "185.228.168.168", "185.228.169.168", "2a0d:2a00:1::", "2a0d:2a00:2::", "family-filter-dns.cleanbrowsing.org")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Adult | DnsBlockCategories.Proxies | DnsBlockCategories.VPN
        },

        new("Cloudflare", "1.1.1.1", "1.0.0.1", "2606:4700:4700::1111", "2606:4700:4700::1001", "cloudflare-dns.com")
        {
            BlockCategories = DnsBlockCategories.Unfiltered
        },
        new("Cloudflare", "1.1.1.2", "1.0.0.2", "2606:4700:4700::1112", "2606:4700:4700::1002", "security.cloudflare-dns.com")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing
        },
        new("Cloudflare", "1.1.1.3", "1.0.0.3", "2606:4700:4700::1113", "2606:4700:4700::1003", "family.cloudflare-dns.com")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Adult
        },

        new("DNS0", "193.110.81.0", "185.253.5.0", "2a0f:fc80::", "2a0f:fc81::", "dns0.eu")
        {
            BlockCategories = DnsBlockCategories.Unfiltered,
        },

        new("Google", "8.8.8.8", "8.8.4.4", "2001:4860:4860::8888", "2001:4860:4860::8844", "dns.google")
        {
            BlockCategories = DnsBlockCategories.Unfiltered,
        },

        new("Mullvad", "194.242.2.2", "0.0.0.0", "2a07:e340::2", "0.0.0.0", "dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Unfiltered,
        },
        new("Mullvad", "194.242.2.3", "0.0.0.0", "2a07:e340::3", "0.0.0.0", "adblock.dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Ads | DnsBlockCategories.Tracking
        },
        new("Mullvad", "194.242.2.4", "0.0.0.0", "2a07:e340::4", "0.0.0.0", "base.dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking
        },
        new("Mullvad", "194.242.2.5", "0.0.0.0", "2a07:e340::5", "0.0.0.0", "extended.dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.SocialMedia
        },
        new("Mullvad", "194.242.2.6", "0.0.0.0", "2a07:e340::6", "0.0.0.0", "family.dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.Adult | DnsBlockCategories.Gambling
        },
        new("Mullvad", "194.242.2.7", "0.0.0.0", "2a07:e340::7", "0.0.0.0", "all.dns.mullvad.net")
        {
            BlockCategories = DnsBlockCategories.Malware | DnsBlockCategories.Phishing | DnsBlockCategories.Ads | DnsBlockCategories.Tracking | DnsBlockCategories.SocialMedia | DnsBlockCategories.Adult | DnsBlockCategories.Gambling
        },

        new("OpenDNS", "208.67.222.222", "208.67.220.220", "2620:119:35::35", "2620:119:53::53", "dns.opendns.com")
        {
            BlockCategories = DnsBlockCategories.Unfiltered
        },
        new("OpenDNS", "208.67.222.123", "208.67.220.123", "2620:119:35::123", "2620:119:53::123", "familyshield.opendns.com")
        {
            BlockCategories = DnsBlockCategories.Phishing | DnsBlockCategories.Adult | DnsBlockCategories.Proxies
        },

        new("Quad9", "9.9.9.9", "149.112.112.112", "2620:fe::fe", "2620:fe::9", "dns.quad9.net")
        {
            BlockCategories = DnsBlockCategories.Malware
        },
        new("Quad9", "9.9.9.11", "149.112.112.11", "2620:fe::11", "2620:fe::fe:11", "dns11.quad9.net")
        {
            BlockCategories = DnsBlockCategories.Malware,
            Notes = "Secured w/ECS: Malware blocking, DNSSEC Validation, ECS enabled"
        },
        new("Quad9", "9.9.9.10", "149.112.112.10", "2620:fe::10", "2620:fe::fe:10", "dns10.quad9.net")
        {
            BlockCategories = DnsBlockCategories.Unfiltered
        },
    ];
}
