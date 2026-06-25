// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.PriorityFallback.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that under the default priority-fallback strategy, the first child that resolves wins.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void TryGetRate_WhenBothChildrenHaveRate_ShouldReturnFirstChild()
    {
        AggregatingExchangeRateProvider agg = new(new[]
        {
            Named("First", ("USD", "AUD", D1, 1.50m)),
            Named("Second", ("USD", "AUD", D1, 1.60m)),
        });

        bool found = agg.TryGetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual("First", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a child that cannot satisfy the request falls through to the next.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFirstChildMisses_ShouldFallThroughToSecond()
    {
        AggregatingExchangeRateProvider agg = new(new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.60m)),
        });

        bool found = agg.TryGetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual("Second", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that when no child can satisfy the request, the lookup returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenAllChildrenMiss_ShouldReturnFalse()
    {
        AggregatingExchangeRateProvider agg = new(new[] { Named("First"), Named("Second") });

        Assert.IsFalse(agg.TryGetRate("USD", "AUD", D1, ExchangeRateLookupOptions.Exact, out _));
    }

    /// <summary>
    /// Verifies that the range lookup returns the first child's non-empty result.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenFirstChildEmpty_ShouldFallThroughToSecond()
    {
        AggregatingExchangeRateProvider agg = new(new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.60m)),
        });

        IReadOnlyList<ExchangeRate> rates = [.. await agg.GetRatesAsync("USD", "AUD", D1, D1)];

        Assert.HasCount(1, rates);
        Assert.AreEqual("Second", rates[0].Provider);
    }

    /// <summary>
    /// Verifies that the range lookup returns an empty list when no child has data.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenNoChildHasData_ShouldReturnEmpty()
    {
        AggregatingExchangeRateProvider agg = new(new[] { Named("First") });

        IReadOnlyList<ExchangeRate> rates = [.. await agg.GetRatesAsync("USD", "AUD", D1, D1)];

        Assert.IsEmpty(rates);
    }
}
