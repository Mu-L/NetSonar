using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetSonar.Tests;

[TestClass]
public sealed partial class LocalizationCatalogTests
{
    [TestMethod]
    public void Catalogs_AllMatchNeutralKeysAndPlaceholders()
    {
        var localizationDirectory = Path.Combine(AppContext.BaseDirectory, "Localization");
        var catalogPaths = Directory.GetFiles(localizationDirectory, "Strings*.resx");
        Assert.AreEqual(14, catalogPaths.Length, "Unexpected localization catalog count.");

        var neutralPath = Path.Combine(localizationDirectory, "Strings.resx");
        var neutral = LoadCatalog(neutralPath);

        foreach (var catalogPath in catalogPaths)
        {
            var catalog = LoadCatalog(catalogPath);
            CollectionAssert.AreEquivalent(
                neutral.Keys.ToArray(),
                catalog.Keys.ToArray(),
                $"Resource-key mismatch in {Path.GetFileName(catalogPath)}.");

            foreach (var (key, neutralValue) in neutral)
            {
                CollectionAssert.AreEquivalent(
                    ExtractPlaceholders(neutralValue),
                    ExtractPlaceholders(catalog[key]),
                    $"Composite-format placeholder mismatch for '{key}' in {Path.GetFileName(catalogPath)}.");
            }
        }
    }

    private static Dictionary<string, string> LoadCatalog(string path)
    {
        var entries = XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(element => new
            {
                Name = (string?)element.Attribute("name") ?? string.Empty,
                Value = element.Element("value")?.Value ?? string.Empty,
            })
            .ToArray();

        var duplicateKeys = entries
            .GroupBy(entry => entry.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert.AreEqual(0, duplicateKeys.Length,
            $"Duplicate resource keys in {Path.GetFileName(path)}: {string.Join(", ", duplicateKeys)}");

        var emptyKeys = entries
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => entry.Name)
            .ToArray();
        Assert.AreEqual(0, emptyKeys.Length,
            $"Empty resource values in {Path.GetFileName(path)}: {string.Join(", ", emptyKeys)}");

        return entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
    }

    private static string[] ExtractPlaceholders(string value)
    {
        return CompositePlaceholderRegex()
            .Matches(value)
            .Select(match => match.Value)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    [GeneratedRegex(@"\{\d+(?:[^{}]*)?\}", RegexOptions.CultureInvariant)]
    private static partial Regex CompositePlaceholderRegex();
}
