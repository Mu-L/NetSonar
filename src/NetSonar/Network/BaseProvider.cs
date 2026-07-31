using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace NetSonar.Avalonia.Network;

public record BaseProvider
{
    /// <summary>
    /// The protocol used to probe the provider.
    /// </summary>
    public required ServiceProtocolType ProtocolType { get; init; }

    /// <summary>
    /// The name of the provider.
    /// </summary>
    public required string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// The hostname, IP address, or absolute URL of the provider.
    /// </summary>
    public required string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// The provider port, or zero when the address contains the port or the protocol default applies.
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// The notes of the DNS.
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>
    /// The address accepted by a ping service.
    /// </summary>
    public string Address
    {
        get
        {
            if (Port <= IPEndPoint.MinPort
                || Uri.TryCreate(Hostname, UriKind.Absolute, out _))
            {
                return Hostname;
            }

            return IPAddress.TryParse(Hostname, out var address)
                   && address.AddressFamily == AddressFamily.InterNetworkV6
                ? $"[{Hostname}]:{Port}"
                : $"{Hostname}:{Port}";
        }
    }

    /// <summary>
    /// The formated description.
    /// </summary>
    public virtual string FormatedDescription
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Notes))
            {
                return ProviderName;
            }
            else
            {
                return Notes.StartsWith(ProviderName, StringComparison.OrdinalIgnoreCase)
                    ? Notes
                    : $"{ProviderName}: {Notes}";
            }
        }
    }

    public BaseProvider()
    {
    }

    [SetsRequiredMembers]
    public BaseProvider(ServiceProtocolType protocolType,
        string providerName,
        string hostname,
        string notes = "",
        int port = IPEndPoint.MinPort)
    {
        ProviderName = providerName;
        ProtocolType = protocolType;
        Hostname = hostname;
        Port = port;
        Notes = notes;
    }

    /// <summary>
    /// Credential-free public endpoints suitable for protocol probes.
    /// DNS uses its specialized provider catalogue.
    /// </summary>
    public static BaseProvider[] PublicHosts { get; } =
    [
        new(ServiceProtocolType.TLS, "BadSSL", "badssl.com", "Valid public TLS test endpoint", PingableService.DefaultTlsPort),
        new(ServiceProtocolType.TLS, "Cloudflare", "cloudflare.com", "Public HTTPS TLS endpoint", PingableService.DefaultTlsPort),
        new(ServiceProtocolType.TLS, "Google", "google.com", "Public HTTPS TLS endpoint", PingableService.DefaultTlsPort),
        new(ServiceProtocolType.TLS, "GitHub", "github.com", "Public HTTPS TLS endpoint", PingableService.DefaultTlsPort),
        new(ServiceProtocolType.TLS, "Let's Encrypt", "valid-isrgrootx1.letsencrypt.org", "ISRG Root X1 certificate-chain test", PingableService.DefaultTlsPort),

        new(ServiceProtocolType.NTP, "NTP Pool", "pool.ntp.org", "Global round-robin pool (pool)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "0.pool.ntp.org", "Global round-robin pool (0)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "1.pool.ntp.org", "Global round-robin pool (1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "2.pool.ntp.org", "Global round-robin pool (2)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "3.pool.ntp.org", "Global round-robin pool (3)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "africa.pool.ntp.org", "Continental pool (africa)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "asia.pool.ntp.org", "Continental pool (asia)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "europe.pool.ntp.org", "Continental pool (europe)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "north-america.pool.ntp.org", "Continental pool (north-america)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "oceania.pool.ntp.org", "Continental pool (oceania)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NTP Pool", "south-america.pool.ntp.org", "Continental pool (south-america)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Google", "time.google.com", "Leap-smeared (time)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Google", "time1.google.com", "Leap-smeared (time1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Google", "time2.google.com", "Leap-smeared (time2)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Google", "time3.google.com", "Leap-smeared (time3)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Google", "time4.google.com", "Leap-smeared (time4)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Cloudflare", "time.cloudflare.com", "Supports NTS (time)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Apple", "time.apple.com", "Apple (time)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Microsoft", "time.windows.com", "Microsoft (time)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Meta", "time.facebook.com", "Meta (time)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Meta", "time1.facebook.com", "Meta (time1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Meta", "time2.facebook.com", "Meta (time2)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Meta", "time3.facebook.com", "Meta (time3)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Meta", "time4.facebook.com", "Meta (time4)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "Meta", "time5.facebook.com", "Meta (time5)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Amazon", "time.aws.com", "Amazon Time Sync Service (time)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "Canonical", "ntp.ubuntu.com", "Canonical (ntp)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "NIST", "time.nist.gov", "US National Institute of Standards and Technology (time)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NIST", "time-a-g.nist.gov", "Stratum 1, Gaithersburg MD (time-a-g)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NIST", "time-b-g.nist.gov", "Stratum 1, Gaithersburg MD (time-b-g)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NIST", "time-c-g.nist.gov", "Stratum 1, Gaithersburg MD (time-c-g)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NIST", "time-d-g.nist.gov", "Stratum 1, Gaithersburg MD (time-d-g)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "NPL", "ntp1.npl.co.uk", "UK National Physical Laboratory, Stratum 1 (ntp1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "NPL", "ntp2.npl.co.uk", "UK National Physical Laboratory, Stratum 1 (ntp2)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "PTB", "ptbtime1.ptb.de", "Germany Physikalisch-Technische Bundesanstalt, Stratum 1 (ptbtime1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "PTB", "ptbtime2.ptb.de", "Germany Physikalisch-Technische Bundesanstalt, Stratum 1 (ptbtime2)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "NICT", "ntp.nict.jp", "Japan National Institute of Information and Communications Technology, Stratum 1 (ntp)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.NTP, "VNIIFTRI", "ntp1.vniiftri.ru", "Russia, Stratum 1 (ntp1)", PingableService.DefaultNtpPort),
        new(ServiceProtocolType.NTP, "VNIIFTRI", "ntp2.vniiftri.ru", "Russia, Stratum 1 (ntp2)", PingableService.DefaultNtpPort),

        new(ServiceProtocolType.HTTP, "Google", "https://connectivitycheck.gstatic.com/generate_204", "Android and Chromium connectivity check"),
        new(ServiceProtocolType.HTTP, "Microsoft", "http://www.msftconnecttest.com/connecttest.txt", "Windows NCSI connectivity check"),
        new(ServiceProtocolType.HTTP, "Apple", "http://captive.apple.com/hotspot-detect.html", "Apple captive portal connectivity check"),
        new(ServiceProtocolType.HTTP, "Mozilla", "http://detectportal.firefox.com/canonical.html", "Firefox captive portal connectivity check"),
        new(ServiceProtocolType.HTTP, "Canonical", "http://connectivity-check.ubuntu.com", "Ubuntu NetworkManager connectivity check"),
        new(ServiceProtocolType.HTTP, "Cloudflare", "https://www.cloudflare.com/cdn-cgi/trace", "Cloudflare network diagnostics endpoint"),
        new(ServiceProtocolType.HTTP, "GitHub", "https://github.com", "GitHub public website"),

        new(ServiceProtocolType.WebSocket, "Postman", "wss://ws.postman-echo.com/raw", "WebSocket echo service"),
        new(ServiceProtocolType.WebSocket, "WebSocket.org", "wss://echo.websocket.org", "WebSocket echo service"),

        new(ServiceProtocolType.SSH, "GitHub", "github.com", "Public Git SSH endpoint", PingableService.DefaultSshPort),
        new(ServiceProtocolType.SSH, "GitHub", "ssh.github.com", "Git SSH over the HTTPS port", 443),
        new(ServiceProtocolType.SSH, "GitLab", "gitlab.com", "Public Git SSH endpoint", PingableService.DefaultSshPort),
        new(ServiceProtocolType.SSH, "GitLab", "altssh.gitlab.com", "Git SSH over the HTTPS port", 443),
        new(ServiceProtocolType.SSH, "Bitbucket", "bitbucket.org", "Public Git SSH endpoint", PingableService.DefaultSshPort),
        new(ServiceProtocolType.SSH, "Bitbucket", "altssh.bitbucket.org", "Git SSH over the HTTPS port", 443),
        new(ServiceProtocolType.SSH, "Azure DevOps", "ssh.dev.azure.com", "Public Azure Repos SSH endpoint", PingableService.DefaultSshPort),
        new(ServiceProtocolType.SSH, "Codeberg", "codeberg.org", "Public Forgejo Git SSH endpoint", PingableService.DefaultSshPort),

        new(ServiceProtocolType.SMTP, "Google", "smtp.gmail.com", "SMTP with STARTTLS", 587),
        new(ServiceProtocolType.SMTP, "Microsoft", "smtp-mail.outlook.com", "SMTP with STARTTLS", 587),
        new(ServiceProtocolType.SMTP, "Yahoo", "smtp.mail.yahoo.com", "SMTP with STARTTLS", 587),
        new(ServiceProtocolType.SMTP, "Apple", "smtp.mail.me.com", "iCloud Mail SMTP with STARTTLS", 587),
        new(ServiceProtocolType.SMTP, "Zoho", "smtp.zoho.com", "SMTP with STARTTLS", 587),
        new(ServiceProtocolType.SMTP, "Fastmail", "smtp.fastmail.com", "SMTP with STARTTLS", 587),

        new(ServiceProtocolType.IMAP, "Google", "imap.gmail.com", "IMAP over implicit TLS", PingableService.DefaultImapTlsPort),
        new(ServiceProtocolType.IMAP, "Microsoft", "outlook.office365.com", "IMAP over implicit TLS", PingableService.DefaultImapTlsPort),
        new(ServiceProtocolType.IMAP, "Yahoo", "imap.mail.yahoo.com", "IMAP over implicit TLS", PingableService.DefaultImapTlsPort),
        new(ServiceProtocolType.IMAP, "Apple", "imap.mail.me.com", "iCloud Mail IMAP over implicit TLS", PingableService.DefaultImapTlsPort),
        new(ServiceProtocolType.IMAP, "Zoho", "imap.zoho.com", "IMAP over implicit TLS", PingableService.DefaultImapTlsPort),
        new(ServiceProtocolType.IMAP, "Fastmail", "imap.fastmail.com", "IMAP over implicit TLS", PingableService.DefaultImapTlsPort),

        new(ServiceProtocolType.MQTT, "HiveMQ", "broker.hivemq.com", "Public MQTT broker", PingableService.DefaultMqttPort),
        new(ServiceProtocolType.MQTT, "Eclipse Mosquitto", "test.mosquitto.org", "Public MQTT test broker", PingableService.DefaultMqttPort),
        new(ServiceProtocolType.MQTT, "EMQX", "broker.emqx.io", "Public MQTT test broker", PingableService.DefaultMqttPort),
        new(ServiceProtocolType.MQTT, "Eclipse IoT", "mqtt.eclipseprojects.io", "Public Mosquitto sandbox", PingableService.DefaultMqttPort),

        new(ServiceProtocolType.STUN, "Cloudflare", "stun.cloudflare.com", "Public STUN endpoint", PingableService.DefaultStunPort),
        //new(ServiceProtocolType.STUN, "Cloudflare", "stun.cloudflare.com", "Public STUN alternate port", 53),
        new(ServiceProtocolType.STUN, "Google", "stun.l.google.com", "Public STUN test endpoint", 19302),
        new(ServiceProtocolType.STUN, "Google", "stun1.l.google.com", "Public STUN test endpoint", 19302),
        new(ServiceProtocolType.STUN, "Twilio", "global.stun.twilio.com", "Global STUN endpoint", PingableService.DefaultStunPort),

        new(ServiceProtocolType.SIP, "SIP2SIP", "proxy.sipthor.net", "Public SIP proxy", PingableService.DefaultSipPort),
        new(ServiceProtocolType.SIP, "iptel.org", "sip.iptel.org", "Public SIP proxy", PingableService.DefaultSipPort),
        new(ServiceProtocolType.SIP, "Linphone", "sip.linphone.org", "Public SIP proxy", PingableService.DefaultSipPort),
    ];
}
