// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.TryByNumericCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

public partial class CurrencyLookupServiceTests
{

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
