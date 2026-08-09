using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSonar.Avalonia.Models;
using NetSonar.Avalonia.Network;

namespace NetSonar.Tests;

[TestClass]
public sealed class ClipboardPasteParserTests
{
    [TestMethod]
    public void Parse_FixedColumnsWithEmbeddedPort_PreservesIntervalAndMetadata()
    {
        const string text = "IP\tPort\tInterval\tDescription\tGroup\n"
                            + "server.example:443\t\t30\tWeb server\tLAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        Assert.AreEqual(1, result.SkippedCount);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.TLS, service.ProtocolType);
        Assert.AreEqual("server.example:443", service.IpAddressOrUrl);
        Assert.AreEqual(30, service.PingEverySeconds);
        Assert.AreEqual("Web server", service.Description);
        Assert.AreEqual("LAN", service.Group);
    }

    [TestMethod]
    public void Parse_CompactColumnsWithEmbeddedPort_PreservesIntervalAndMetadata()
    {
        const string text = "server.example:443\t30\tWeb server\tLAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(30, service.PingEverySeconds);
        Assert.AreEqual("Web server", service.Description);
        Assert.AreEqual("LAN", service.Group);
    }

    [TestMethod]
    public void Parse_EmbeddedPortWithLargeInterval_TreatsSecondColumnAsInterval()
    {
        const string text = "server.example:80\t100000";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        Assert.AreEqual(0, result.SkippedCount);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.HTTP, service.ProtocolType);
        Assert.AreEqual("server.example:80", service.IpAddressOrUrl);
        Assert.AreEqual(100000, service.PingEverySeconds);
    }

    [TestMethod]
    public void Parse_ExplicitInvalidInterval_SkipsRow()
    {
        const string text = "IP\tPort\tInterval\tDescription\tGroup\n"
                            + "192.168.1.10\t80\tnot-a-number\tWeb server\tLAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(0, result.Services.Count);
        Assert.AreEqual(2, result.SkippedCount);
    }

    [TestMethod]
    [DoNotParallelize]
    public void Parse_LocalizedAndInvariantDecimalIntervals_PreservesColumnMapping()
    {
        const string text = "server.example:80\t0,5\tLocalized interval\tLAN\n"
                            + "server.example:81\t0.75\tInvariant interval\tWAN";
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-PT");

            var result = ClipboardPasteParser.Parse(text);

            Assert.AreEqual(2, result.Services.Count);
            Assert.AreEqual(0, result.SkippedCount);
            Assert.AreEqual(0.5, result.Services[0].PingEverySeconds);
            Assert.AreEqual("Localized interval", result.Services[0].Description);
            Assert.AreEqual("LAN", result.Services[0].Group);
            Assert.AreEqual(0.75, result.Services[1].PingEverySeconds);
            Assert.AreEqual("Invariant interval", result.Services[1].Description);
            Assert.AreEqual("WAN", result.Services[1].Group);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Parse_MixedRowLayouts_ImportsEveryValidService()
    {
        const string text = "192.168.1.130\t\t30\tWeb Server\tLan\n"
                            + "192.168.1.130:40\tWeb Server\tLan\n"
                            + "http://192.168.1.130:50\tWeb Server\tLan\n"
                            + "dns://8.8.8.8|30|Google DNS|DNS\n"
                            + "1.1.1.1:53|30|Cloudflare DNS|DNS\n"
                            + "google.com:81|Google|HTTP\n"
                            + "smtp.google.com:25|Google SMTP|SMTP";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(7, result.Services.Count);
        Assert.AreEqual(0, result.SkippedCount);
        CollectionAssert.AreEqual(
            new[]
            {
                ServiceProtocolType.ICMP,
                ServiceProtocolType.TCP,
                ServiceProtocolType.HTTP,
                ServiceProtocolType.DNS,
                ServiceProtocolType.DNS,
                ServiceProtocolType.TCP,
                ServiceProtocolType.SMTP
            },
            result.Services.Select(service => service.ProtocolType).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Web Server", "Web Server", "Web Server", "Google DNS", "Cloudflare DNS", "Google", "Google SMTP" },
            result.Services.Select(service => service.Description).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Lan", "Lan", "Lan", "DNS", "DNS", "HTTP", "SMTP" },
            result.Services.Select(service => service.Group).ToArray());
    }

    [TestMethod]
    public void Parse_BareIpv6Address_CreatesValidIcmpService()
    {
        const string text = "2001:db8::1";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.ICMP, service.ProtocolType);
        Assert.AreEqual(text, service.IpAddressOrUrl);
        Assert.IsTrue(service.Validate());
    }

    [TestMethod]
    public void Parse_BracketedIpv6Address_CreatesNormalizedIcmpService()
    {
        const string text = "[2001:db8::1]";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.ICMP, service.ProtocolType);
        Assert.AreEqual("2001:db8::1", service.IpAddressOrUrl);
        Assert.IsTrue(service.Validate());
    }

    [TestMethod]
    public void Parse_BracketedIpv6DefaultTlsEndpoint_CreatesTlsService()
    {
        const string text = "[2001:db8::1]:443";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.TLS, service.ProtocolType);
        Assert.AreEqual(text, service.IpAddressOrUrl);
        Assert.IsTrue(service.Validate());
    }

    [TestMethod]
    [DataRow("icmp://example.com", ServiceProtocolType.ICMP, "example.com")]
    [DataRow("tcp://example.com:80", ServiceProtocolType.TCP, "example.com:80")]
    [DataRow("udp://example.com:53", ServiceProtocolType.UDP, "example.com:53")]
    [DataRow("tls://example.com:443", ServiceProtocolType.TLS, "example.com:443")]
    [DataRow("dns://1.1.1.1", ServiceProtocolType.DNS, "1.1.1.1")]
    [DataRow("ntp://time.cloudflare.com", ServiceProtocolType.NTP, "time.cloudflare.com")]
    [DataRow("http://example.com/status", ServiceProtocolType.HTTP, "http://example.com/status")]
    [DataRow("https://example.com/status", ServiceProtocolType.HTTP, "https://example.com/status")]
    [DataRow("ws://example.com/socket", ServiceProtocolType.WebSocket, "ws://example.com/socket")]
    [DataRow("wss://example.com/socket", ServiceProtocolType.WebSocket, "wss://example.com/socket")]
    [DataRow("ssh://example.com:22", ServiceProtocolType.SSH, "example.com:22")]
    [DataRow("smtp://example.com:25", ServiceProtocolType.SMTP, "example.com:25")]
    [DataRow("imap://example.com:143", ServiceProtocolType.IMAP, "example.com:143")]
    [DataRow("mqtt://example.com:1883", ServiceProtocolType.MQTT, "example.com:1883")]
    [DataRow("stun://example.com:3478", ServiceProtocolType.STUN, "example.com:3478")]
    [DataRow("sip://example.com:5060", ServiceProtocolType.SIP, "example.com:5060")]
    public void Parse_ExplicitProtocolScheme_CreatesExpectedService(
        string text,
        ServiceProtocolType expectedProtocol,
        string expectedAddress)
    {
        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.AreEqual(expectedProtocol, result.Services[0].ProtocolType);
        Assert.AreEqual(expectedAddress, result.Services[0].IpAddressOrUrl);
    }

    [TestMethod]
    public void Parse_ExplicitProtocolWithCompactFields_PreservesIntervalAndMetadata()
    {
        const string text = "icmp://[2001:db8::1]|30|Gateway|LAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        Assert.AreEqual(0, result.SkippedCount);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.ICMP, service.ProtocolType);
        Assert.AreEqual("2001:db8::1", service.IpAddressOrUrl);
        Assert.AreEqual(30, service.PingEverySeconds);
        Assert.AreEqual("Gateway", service.Description);
        Assert.AreEqual("LAN", service.Group);
    }

    [TestMethod]
    public void Parse_UnknownProtocolScheme_SkipsRow()
    {
        var result = ClipboardPasteParser.Parse("ftp://example.com");

        Assert.AreEqual(0, result.Services.Count);
        Assert.AreEqual(1, result.SkippedCount);
    }

    [TestMethod]
    [DataRow("example.com:http")]
    [DataRow("example.com:65536")]
    [DataRow("example.com:99999")]
    [DataRow("tcp://example.com:99999")]
    [DataRow("udp://example.com:http")]
    [DataRow("dns://example.com:99999")]
    [DataRow("tcp://[2001:db8::1]")]
    public void Parse_MalformedSocketEndpoint_SkipsRow(string text)
    {
        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(0, result.Services.Count);
        Assert.AreEqual(1, result.SkippedCount);
    }

    [TestMethod]
    [DataRow("example.com\t0", ServiceProtocolType.ICMP, "example.com")]
    [DataRow("example.com:0", ServiceProtocolType.ICMP, "example.com")]
    [DataRow("[2001:db8::1]:0", ServiceProtocolType.ICMP, "2001:db8::1")]
    [DataRow("example.com\t", ServiceProtocolType.ICMP, "example.com")]
    [DataRow("example.com", ServiceProtocolType.ICMP, "example.com")]
    [DataRow("example.com\t22", ServiceProtocolType.SSH, "example.com:22")]
    [DataRow("example.com\t25", ServiceProtocolType.SMTP, "example.com:25")]
    [DataRow("example.com\t53", ServiceProtocolType.DNS, "example.com:53")]
    [DataRow("example.com\t80", ServiceProtocolType.HTTP, "example.com:80")]
    [DataRow("example.com\t123", ServiceProtocolType.NTP, "example.com:123")]
    [DataRow("example.com\t143", ServiceProtocolType.IMAP, "example.com:143")]
    [DataRow("example.com\t443", ServiceProtocolType.TLS, "example.com:443")]
    [DataRow("example.com\t465", ServiceProtocolType.TLS, "example.com:465")]
    [DataRow("example.com\t587", ServiceProtocolType.SMTP, "example.com:587")]
    [DataRow("example.com\t853", ServiceProtocolType.TLS, "example.com:853")]
    [DataRow("example.com\t993", ServiceProtocolType.IMAP, "example.com:993")]
    [DataRow("example.com\t1883", ServiceProtocolType.MQTT, "example.com:1883")]
    [DataRow("example.com\t3478", ServiceProtocolType.STUN, "example.com:3478")]
    [DataRow("example.com\t5060", ServiceProtocolType.SIP, "example.com:5060")]
    [DataRow("example.com\t5061", ServiceProtocolType.TLS, "example.com:5061")]
    [DataRow("example.com\t5349", ServiceProtocolType.TLS, "example.com:5349")]
    [DataRow("example.com\t8008", ServiceProtocolType.HTTP, "example.com:8008")]
    [DataRow("example.com\t8080", ServiceProtocolType.HTTP, "example.com:8080")]
    [DataRow("example.com\t8443", ServiceProtocolType.TLS, "example.com:8443")]
    [DataRow("example.com\t8883", ServiceProtocolType.TLS, "example.com:8883")]
    [DataRow("example.com\t19302", ServiceProtocolType.STUN, "example.com:19302")]
    [DataRow("example.com\t12345", ServiceProtocolType.TCP, "example.com:12345")]
    public void Parse_PortValue_InfersExpectedProtocol(
        string text,
        ServiceProtocolType expectedProtocol,
        string expectedAddress)
    {
        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        Assert.AreEqual(0, result.SkippedCount);
        Assert.AreEqual(expectedProtocol, result.Services[0].ProtocolType);
        Assert.AreEqual(expectedAddress, result.Services[0].IpAddressOrUrl);
    }

    [TestMethod]
    public void Parse_EmbeddedDefaultPort_InfersProtocolAndPreservesCompactFields()
    {
        const string text = "example.com:53|30|Resolver|LAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.DNS, service.ProtocolType);
        Assert.AreEqual("example.com:53", service.IpAddressOrUrl);
        Assert.AreEqual(30, service.PingEverySeconds);
        Assert.AreEqual("Resolver", service.Description);
        Assert.AreEqual("LAN", service.Group);
    }

    [TestMethod]
    public void Parse_ZeroPortInFixedLayout_CreatesIcmpAndPreservesFields()
    {
        const string text = "example.com\t0\t30\tHost\tLAN";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.ICMP, service.ProtocolType);
        Assert.AreEqual("example.com", service.IpAddressOrUrl);
        Assert.AreEqual(30, service.PingEverySeconds);
        Assert.AreEqual("Host", service.Description);
        Assert.AreEqual("LAN", service.Group);
    }
}
