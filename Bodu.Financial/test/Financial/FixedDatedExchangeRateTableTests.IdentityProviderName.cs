// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateTableTests.IdentityProviderName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateTableTests
{
    /// <summary>
    /// Verifies that the same-currency identity provider name is exposed publicly so audit consumers can
    /// filter on it without depending on a magic string literal.
    /// </summary>
    [TestMethod]
    public void IdentityProviderName_WhenExposedPublicly_ShouldBeAccessibleAndMatchSyntheticRateProvider()
    {
        FixedDatedExchangeRateTable table = new(
        [
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "RBA"),
        ]);

        ExchangeRateLookupResult result = table.GetRate("USD", "USD", new DateOnly(2024, 6, 30), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(FixedDatedExchangeRateTable.IdentityProviderName, result.Rate.Provider);
        Assert.AreEqual(1m, result.Rate.Rate);
    }
}
