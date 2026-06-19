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
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("First"),
            Named("Second", ("USD", "AUD", D1, 1.6m)),
        };

        IReadOnlyList<ExchangeRate> rates = await PriorityFallbackStrategy.Instance.AggregateRangeAsync("USD", "AUD", D1, D1, candidates, default);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual("Second", rates[0].Provider);
    }
}
