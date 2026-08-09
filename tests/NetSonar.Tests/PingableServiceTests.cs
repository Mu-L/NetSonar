using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSonar.Avalonia.Network;

namespace NetSonar.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PingableServiceTests
{
    [TestMethod]
    public void ParseFromString_LocalizedAndInvariantSettings_UsesExpectedValues()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-PT");

            var localized = PingableService.ParseFromString("localhost | 0,5 | 0,75");
            var invariant = PingableService.ParseFromString("localhost | 0.75 | 1.25");

            Assert.AreEqual(0.5, localized.PingEverySeconds);
            Assert.AreEqual(0.75, localized.TimeoutSeconds);
            Assert.AreEqual(0.75, invariant.PingEverySeconds);
            Assert.AreEqual(1.25, invariant.TimeoutSeconds);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
