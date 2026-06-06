// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="CurrencyLookupService" /> resolves currencies by ISO code, numeric code, symbol, region, and
/// culture.
/// </summary>
[TestClass]
public class CurrencyLookupServiceTests
{
    /// <summary>
    /// Verifies that <see cref="CurrencyLookupService.TryByIsoCode(string, out CurrencyInfo)" /> resolves a shipped
    /// currency.
    /// </summary>
    [TestMethod]
    public void TryByIsoCode_WhenShippedCurrency_ShouldResolve()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.IsTrue(lookup.TryByIsoCode("USD", out CurrencyInfo usd));
        Assert.AreEqual("USD", usd.IsoCode);
        Assert.IsFalse(lookup.TryByIsoCode("ZZZ", out _));
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyLookupService.TryByCurrencyCode(CurrencyCode, out CurrencyInfo)" /> resolves an
    /// active enum value.
    /// </summary>
    [TestMethod]
    public void TryByCurrencyCode_WhenActiveCode_ShouldResolve()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.IsTrue(lookup.TryByCurrencyCode(CurrencyCode.EUR, out CurrencyInfo eur));
        Assert.AreEqual("EUR", eur.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyLookupService.TryByNumericCode(int, out CurrencyInfo)" /> resolves a shipped
    /// currency by its ISO 4217 numeric code.
    /// </summary>
    [TestMethod]
    public void TryByNumericCode_WhenShippedNumericCode_ShouldResolve()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.IsTrue(lookup.TryByNumericCode(840, out CurrencyInfo usd));
        Assert.AreEqual("USD", usd.IsoCode);
        Assert.IsFalse(lookup.TryByNumericCode(99999, out _));
    }

    /// <summary>
    /// Verifies that symbol and region lookups resolve a custom currency registered with display metadata.
    /// </summary>
    [TestMethod]
    public void TryBySymbolAndRegion_WhenCustomCurrencyRegistered_ShouldResolve()
    {
        CurrencyRegistry.Replace(new CurrencyInfo("XQS", 2, 0m, false, null, null)
        {
            Symbol = "Ξ",
            RegionCodes = ["QQ"],
        });

        // Build the service after registration so its lazy indexes include the custom entry.
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.IsTrue(lookup.TryBySymbol("Ξ", out IReadOnlyList<CurrencyInfo> bySymbol));
        Assert.IsTrue(bySymbol.Any(c => c.IsoCode == "XQS"));

        Assert.IsTrue(lookup.TryByRegion("QQ", out IReadOnlyList<CurrencyInfo> byRegion));
        Assert.IsTrue(byRegion.Any(c => c.IsoCode == "XQS"));
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyLookupService.ByCulture(CultureInfo)" /> resolves the region currency for a
    /// specific culture.
    /// </summary>
    [TestMethod]
    public void ByCulture_WhenSpecificCulture_ShouldResolveRegionCurrency()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        IReadOnlyList<CurrencyInfo> currencies = lookup.ByCulture(new CultureInfo("en-US"));

        Assert.IsTrue(currencies.Any(c => c.IsoCode == "USD"));
    }

    /// <summary>
    /// Verifies that every active currency's numeric code resolves back to itself, confirming numeric-code uniqueness
    /// across the shipped catalogue.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryByNumericCode_WhenSweepingActiveCatalogue_ShouldResolveUniquely()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        foreach (CurrencyInfo info in CurrencyRegistry.All.Where(c => !c.IsHistoric && c.NumericCode != 0))
        {
            Assert.IsTrue(lookup.TryByNumericCode(info.NumericCode, out CurrencyInfo resolved), $"No match for numeric {info.NumericCode} ({info.IsoCode}).");
            Assert.AreEqual(info.NumericCode, resolved.NumericCode);
        }
    }
}
