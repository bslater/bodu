// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.IdentityProviderName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class FixedDatedRateProviderTests
{
    /// <summary>
    /// Verifies that the same-currency identity provider name is exposed publicly so audit consumers can
    /// filter on it without depending on a magic string literal.
    /// </summary>
    [TestMethod]
    public void IdentityProviderName_WhenExposedPublicly_ShouldBeAccessibleAndMatchSyntheticRateProvider()
    {
        FixedDatedRateProvider table = new(
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
        ]);

        RateLookupResult result = table.GetRate("USD", "USD", new DateOnly(2024, 6, 30), RateLookupOptions.Exact);

        Assert.AreEqual(FixedDatedRateProvider.IdentityProviderName, result.Rate.Provider);
        Assert.AreEqual(1m, result.Rate.Rate);
    }
}
