// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateTableTests.SameCurrency.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedDatedExchangeRateTableTests
{
    /// <summary>
    /// Verifies that a same-currency lookup with the identity-rate option enabled returns <c>1</c>, the synthetic
    /// <c>Identity</c> provider name, and a zero offset, regardless of whether the table holds any rates for the code.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyAndIdentityEnabled_ShouldReturnIdentityRate()
    {
        FixedDatedExchangeRateTable table = new([]);

        var found = table.TryGetRate(
            "USD",
            "USD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1m, result.Rate.Rate);
        Assert.AreEqual("Identity", result.Rate.Provider);
        Assert.AreEqual(0, result.OffsetDays);
        Assert.AreEqual("USD", result.Rate.FromIsoCode);
        Assert.AreEqual("USD", result.Rate.ToIsoCode);
    }

    /// <summary>
    /// Verifies that a same-currency lookup falls through to the underlying table when the identity-rate option is
    /// disabled, and returns false when no rate is registered for that pair.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSameCurrencyAndIdentityDisabled_ShouldFallThroughToTable()
    {
        FixedDatedExchangeRateTable table = new([]);

        ExchangeRateLookupOptions options = new(
            ExchangeRateDateResolution.Exact,
            AllowSameCurrencyIdentityRate: false);

        var found = table.TryGetRate("USD", "USD", s_d1, options, out _);

        Assert.IsFalse(found);
    }
}
