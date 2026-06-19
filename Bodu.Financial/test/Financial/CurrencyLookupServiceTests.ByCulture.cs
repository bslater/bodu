// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.ByCulture.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CurrencyLookupServiceTests
{

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
    /// Verifies that <see cref="CurrencyLookupService.ByCulture(CultureInfo)" /> returns no currencies for a neutral
    /// culture, which carries no region context.
    /// </summary>
    [TestMethod]
    public void ByCulture_WhenNeutralCulture_ShouldReturnEmpty()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.AreEqual(0, lookup.ByCulture(new CultureInfo("en")).Count);
    }
}
