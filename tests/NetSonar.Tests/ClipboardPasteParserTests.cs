using System.Globalization;
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
        Assert.AreEqual(ServiceProtocolType.TCP, service.ProtocolType);
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
        Assert.AreEqual(ServiceProtocolType.TCP, service.ProtocolType);
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
    public void Parse_BracketedIpv6Endpoint_CreatesTcpService()
    {
        const string text = "[2001:db8::1]:443";

        var result = ClipboardPasteParser.Parse(text);

        Assert.AreEqual(1, result.Services.Count);
        var service = result.Services[0];
        Assert.AreEqual(ServiceProtocolType.TCP, service.ProtocolType);
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
}
