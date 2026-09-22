using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSonar.Avalonia.Network;

namespace NetSonar.Tests;

[TestClass]
public sealed class NmapScannerServiceTests
{
    [TestMethod]
    [DataRow("192.168.1.0/24", true)]
    [DataRow("router.local", true)]
    [DataRow("192.168.1.1-254", true)]
    [DataRow("--script=vuln", false)]
    [DataRow("192.168..1", false)]
    [DataRow("192.168.1.0/40", false)]
    [DataRow("not a target", false)]
    public void IsValidTarget_OnlyAcceptsSupportedTargetExpressions(string target, bool expected)
    {
        Assert.AreEqual(expected, NmapScannerService.IsValidTarget(target));
    }

    [TestMethod]
    public void GetNetworkCidr_ClearsHostBits()
    {
        var cidr = NmapScannerService.GetNetworkCidr(IPAddress.Parse("192.168.4.99"), 24);

        Assert.AreEqual("192.168.4.0/24", cidr);
    }

    [TestMethod]
    public void ParseXml_ReturnsUpHostsAndOpenPorts()
    {
        const string xml = """
                           <?xml version="1.0"?>
                           <nmaprun>
                             <host>
                               <status state="up" />
                               <address addr="192.168.1.10" addrtype="ipv4" />
                               <address addr="AA:BB:CC:DD:EE:FF" addrtype="mac" vendor="Example Vendor" />
                               <hostnames><hostname name="printer.local" /></hostnames>
                               <ports>
                                 <port protocol="tcp" portid="80"><state state="open" /><service name="http" product="nginx" version="1.26" /></port>
                                 <port protocol="tcp" portid="22"><state state="closed" /></port>
                               </ports>
                             </host>
                             <host><status state="down" /><address addr="192.168.1.11" addrtype="ipv4" /></host>
                           </nmaprun>
                           """;

        var hosts = NmapScannerService.ParseXml(xml);

        Assert.HasCount(1, hosts);
        var host = hosts[0];
        Assert.AreEqual("192.168.1.10", host.Address);
        Assert.AreEqual("printer.local", host.Hostname);
        Assert.AreEqual("AA:BB:CC:DD:EE:FF", host.MacAddress);
        Assert.AreEqual("Example Vendor", host.Vendor);
        Assert.HasCount(1, host.Ports);
        Assert.AreEqual(80, host.Ports[0].Number);
        Assert.AreEqual("http", host.Ports[0].Service);
        Assert.AreEqual("nginx 1.26", host.Ports[0].ProductVersion);
    }
}
