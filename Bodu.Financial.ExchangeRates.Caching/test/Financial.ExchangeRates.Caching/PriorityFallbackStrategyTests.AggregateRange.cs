// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallbackStrategyTests.AggregateRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class PriorityFallbackStrategyTests
{
    /// <summary>
    /// Verifies that the synchronous range overload passes over a candidate that cannot carry the pair (one that throws
    /// <see cref="RateSeriesNotFoundException" />) and returns the next candidate's result.
    /// </summary>
    [TestMethod]
    public void AggregateRange_WhenFirstCandidateCannotCarryPair_ShouldFallThrough()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            AudBased("RBA"),
            Named("ECB", ("EUR", "USD", D1, 1.09m)),
        };

        IReadOnlyList<ExchangeRate> rates = PriorityFallbackStrategy.Instance.AggregateRange("EUR", "USD", D1, D1, candidates);

        Assert.HasCount(1, rates);
        Assert.AreEqual("ECB", rates[0].Provider);
    }
}
