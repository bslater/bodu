// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueStrictTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Verifies the shipped currency catalogue against <c>currencies.json</c> entry-by-entry. Generates the
/// expected case set from the JSON so a missing or extra type, a metadata mismatch, or a stale class name is
/// caught at test time rather than slipping into a release.
/// </summary>
[TestClass]
public class CurrencyCatalogueStrictTests
{
    /// <summary>
    /// Returns the full set of currency entries declared in <c>currencies.json</c>.
    /// </summary>
    /// <returns>One entry per declared currency, in source-order.</returns>
    public static IEnumerable<object[]> CurrencyEntries()
    {
        foreach (CatalogueEntry entry in LoadCatalogue())
            yield return new object[] { entry };
    }

    /// <summary>
    /// Verifies that every entry in <c>currencies.json</c> has a corresponding generated tag type with matching
    /// metadata.
    /// </summary>
    /// <param name="entry">The catalogue entry under test.</param>
    [TestMethod]
    [DynamicData(nameof(CurrencyEntries))]
    public void Catalogue_WhenComparingShippedTagAgainstSource_ShouldMatch(CatalogueEntry entry)
    {
        Type? type = typeof(USD).Assembly.GetType($"Bodu.Financial.Currencies.{entry.Iso}");
        Assert.IsNotNull(type, $"Currency {entry.Iso} is declared in currencies.json but no tag type was generated.");

        Assert.IsTrue(type!.IsSealed, $"{entry.Iso} tag type must be sealed.");
        Assert.IsTrue(typeof(ICurrency).IsAssignableFrom(type), $"{entry.Iso} must implement ICurrency.");

        ConstructorInfo[] publicCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsEmpty(publicCtors, $"{entry.Iso} must not expose a public constructor (tag types are non-instantiable).");

        // Read metadata through Money<TCurrency> so default-implemented interface members (e.g. virtual
        // CashRoundingIncrement → 0m) resolve via the validator rather than falling through to a null property
        // on the implementing class.
        Type moneyType = typeof(Money<>).MakeGenericType(type);

        string actualIso = (string)moneyType.GetProperty("IsoCode", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.AreEqual(entry.Iso, actualIso, $"{entry.Iso}.IsoCode mismatch.");

        int actualMinorUnits = (int)moneyType.GetProperty("MinorUnits", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.AreEqual(entry.MinorUnits, actualMinorUnits, $"{entry.Iso}.MinorUnits mismatch.");

        decimal actualCashIncrement = (decimal)moneyType.GetProperty("CashRoundingIncrement", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.AreEqual(entry.CashRoundingIncrement ?? 0m, actualCashIncrement, $"{entry.Iso}.CashRoundingIncrement mismatch.");

        bool actualIsHistoric = (bool)moneyType.GetProperty("IsHistoric", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        Assert.AreEqual(entry.IsHistoric, actualIsHistoric, $"{entry.Iso}.IsHistoric mismatch.");

        var actualDemonetizedOn = (DateOnly?)moneyType.GetProperty("DemonetizedOn", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        DateOnly? expectedDemonetizedOn = entry.DemonetizedOn is null
            ? null
            : DateOnly.ParseExact(entry.DemonetizedOn, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(expectedDemonetizedOn, actualDemonetizedOn, $"{entry.Iso}.DemonetizedOn mismatch.");

        string? actualSuccessor = (string?)moneyType.GetProperty("SuccessorIsoCode", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        Assert.AreEqual(entry.SuccessorIsoCode, actualSuccessor, $"{entry.Iso}.SuccessorIsoCode mismatch.");
    }

    /// <summary>
    /// Verifies that no extra currency tag types exist beyond what <c>currencies.json</c> declares — catches
    /// stale generated files that the regeneration cycle might have missed.
    /// </summary>
    [TestMethod]
    public void Catalogue_WhenComparingShippedTypesAgainstSource_ShouldHaveNoUnexpectedTypes()
    {
        HashSet<string> expected = new(LoadCatalogue().Select(e => e.Iso), StringComparer.Ordinal);
        IEnumerable<string> actual = typeof(USD).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "Bodu.Financial.Currencies"
                && typeof(ICurrency).IsAssignableFrom(t)
                && !t.IsAbstract)
            .Select(t => t.Name);

        List<string> unexpected = new();
        foreach (string code in actual)
        {
            if (!expected.Contains(code))
                unexpected.Add(code);
        }

        Assert.IsEmpty(unexpected, $"Unexpected catalogue types: {string.Join(", ", unexpected)}");
    }

    /// <summary>
    /// Verifies that every entry's ISO code matches its generated class name (case-sensitive).
    /// </summary>
    [TestMethod]
    public void Catalogue_WhenInspectingClassNames_ShouldMatchIsoCode()
    {
        foreach (CatalogueEntry entry in LoadCatalogue())
        {
            Type? type = typeof(USD).Assembly.GetType($"Bodu.Financial.Currencies.{entry.Iso}");
            Assert.IsNotNull(type);
            Assert.AreEqual(entry.Iso, type!.Name);
        }
    }

    /// <summary>
    /// Verifies that the catalogue has no duplicate ISO codes — a structural invariant the generator should
    /// preserve.
    /// </summary>
    [TestMethod]
    public void Catalogue_WhenScannedForDuplicates_ShouldFindNone()
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (CatalogueEntry entry in LoadCatalogue())
        {
            counts.TryGetValue(entry.Iso, out int count);
            counts[entry.Iso] = count + 1;
        }

        IEnumerable<KeyValuePair<string, int>> duplicates = counts.Where(kvp => kvp.Value > 1);
        Assert.IsEmpty(duplicates, $"Duplicate ISO codes: {string.Join(", ", duplicates.Select(kvp => $"{kvp.Key} x{kvp.Value}"))}");
    }

    /// <summary>
    /// Loads the catalogue JSON from the repository (resolved relative to this assembly's source path).
    /// </summary>
    /// <returns>The parsed catalogue entries.</returns>
    private static List<CatalogueEntry> LoadCatalogue()
    {
        string path = ResolveCataloguePath();
        string json = File.ReadAllText(path);
        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        CatalogueRoot? root = JsonSerializer.Deserialize<CatalogueRoot>(json, options);
        Assert.IsNotNull(root);
        Assert.IsNotNull(root!.Currencies);
        return new List<CatalogueEntry>(root.Currencies);
    }

    /// <summary>
    /// Resolves the path of <c>currencies.json</c> by walking up from the test-binary directory until a
    /// directory containing <c>bodu.slnx</c> is found.
    /// </summary>
    /// <returns>The absolute path to <c>currencies.json</c>.</returns>
    private static string ResolveCataloguePath()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "bodu.slnx")))
                return Path.Combine(dir.FullName, "Bodu.Financial", "src", "Financial.Currencies", "currencies.json");
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate currencies.json (bodu.slnx not found in any ancestor).");
    }

    /// <summary>
    /// Root of the catalogue JSON document.
    /// </summary>
    private sealed class CatalogueRoot
    {
        public CatalogueEntry[]? Currencies { get; set; }
    }

    /// <summary>
    /// One row of the catalogue JSON.
    /// </summary>
    public sealed class CatalogueEntry
    {
        public string Iso { get; set; } = string.Empty;
        public int Numeric { get; set; }
        public int MinorUnits { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? CashRoundingIncrement { get; set; }
        public bool IsHistoric { get; set; }
        public string? DemonetizedOn { get; set; }
        public string? SuccessorIsoCode { get; set; }

        public override string ToString() => Iso;
    }
}
