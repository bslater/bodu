// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.SameCurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class FixedDatedRateProviderTests
{
    /// <summary>
    /// Verifies that a same-currency lookup with the identity-rate option enabled returns <c>1</c>, the synthetic
    /// <c>Identity</c> provider name, and a zero offset, regardless of whether the table holds any rates for the code.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyAndIdentityEnabled_ShouldReturnIdentityRate()
    {
        FixedDatedRateProvider table = new([]);

        bool found = table.TryGetRate(
            "USD",
            "USD",
            s_d1,
            RateLookupOptions.Exact,
            out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1m, result.Rate.Rate);
        Assert.AreEqual("Identity", result.Rate.Provider);
        Assert.AreEqual(0, result.OffsetDays);
        Assert.AreEqual(CurrencyCode.USD, result.Rate.From);
        Assert.AreEqual(CurrencyCode.USD, result.Rate.To);
    }

    /// <summary>
    /// Verifies that a same-currency lookup falls through to the underlying table when the identity-rate option is
    /// disabled, and returns false when no rate is registered for that pair.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyAndIdentityDisabled_ShouldFallThroughToTable()
    {
        FixedDatedRateProvider table = new([]);

        RateLookupOptions options = new(
            RateDateResolution.Exact,
            allowSameCurrencyIdentityRate: false);

        bool found = table.TryGetRate("USD", "USD", s_d1, options, out _);

        Assert.IsFalse(found);
    }
}
