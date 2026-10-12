// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategyTests.AggregateRangeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class PriorityFallbackStrategyTests
{
    /// <summary>
    /// Verifies that the range overload returns the first candidate's non-empty result.
    /// </summary>
    [TestMethod]
    public async Task AggregateRangeAsync_WhenFirstCandidateEmpty_ShouldFallThrough()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.6m)),
        };

        IReadOnlyList<ExchangeRate> rates = await PriorityFallbackStrategy.Instance.AggregateRangeAsync("USD", "AUD", D1, D1, candidates, default);

        Assert.HasCount(1, rates);
        Assert.AreEqual("Second", rates[0].Provider);
    }

    /// <summary>
    /// Verifies that the range overload passes over a candidate that cannot carry the pair (one that throws
    /// <see cref="RateSeriesNotFoundException" />) and returns the next candidate's result.
    /// </summary>
    [TestMethod]
    public async Task AggregateRangeAsync_WhenFirstCandidateCannotCarryPair_ShouldFallThrough()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            AudBased("RBA"),
            Named("ECB", ("EUR", "USD", D1, 1.09m)),
        };

        IReadOnlyList<ExchangeRate> rates = await PriorityFallbackStrategy.Instance.AggregateRangeAsync("EUR", "USD", D1, D1, candidates, default);

        Assert.HasCount(1, rates);
        Assert.AreEqual("ECB", rates[0].Provider);
    }
}
