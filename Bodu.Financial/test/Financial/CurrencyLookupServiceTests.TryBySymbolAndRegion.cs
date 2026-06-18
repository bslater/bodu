// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.TryBySymbolAndRegion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CurrencyLookupServiceTests
{

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
}
