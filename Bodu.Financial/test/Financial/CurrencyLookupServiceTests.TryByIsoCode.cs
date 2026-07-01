// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.TryByIsoCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CurrencyLookupServiceTests
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
}
