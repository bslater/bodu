// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyLookupServiceTests.TryByCurrencyCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Financial.Currencies;

public partial class CurrencyLookupServiceTests
{

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
}
