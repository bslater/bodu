// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.Lookups.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CurrencyLookupServiceTests
{

    /// <summary>
    /// Verifies that every lookup reports failure (without throwing) when no currency matches the supplied code,
    /// numeric code, symbol, or region.
    /// </summary>
    [TestMethod]
    public void Lookups_WhenNoMatch_ShouldReturnFalseOrEmpty()
    {
        ICurrencyLookup lookup = new CurrencyLookupService();

        Assert.IsFalse(lookup.TryByCurrencyCode((CurrencyCode)(-1), out _));
        Assert.IsFalse(lookup.TryByNumericCode(99999, out _));
        Assert.IsFalse(lookup.TryBySymbol("☃", out IReadOnlyList<CurrencyInfo> bySymbol));
        Assert.AreEqual(0, bySymbol.Count);
        Assert.IsFalse(lookup.TryByRegion("ZZ", out IReadOnlyList<CurrencyInfo> byRegion));
        Assert.AreEqual(0, byRegion.Count);
    }
}
