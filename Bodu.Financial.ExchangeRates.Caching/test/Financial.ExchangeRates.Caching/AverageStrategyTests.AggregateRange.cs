// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.AggregateRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AverageStrategyTests
{
    /// <summary>
    /// Verifies that the synchronous range overload leaves out a candidate that cannot carry the pair (one that throws
    /// <see cref="RateSeriesNotFoundException" />) and averages the candidates that resolve.
    /// </summary>
    [TestMethod]
    public void AggregateRange_WhenCandidateCannotCarryPair_ShouldAverageTheOthers()
    {
        IReadOnlyList<NamedDatedRateProvider> candidates = new[]
        {
            AudBased("RBA"),
            Named("ECB", ("EUR", "USD", D1, 1.08m)),
            Named("BankB", ("EUR", "USD", D1, 1.10m)),
        };

        IReadOnlyList<ExchangeRate> rates = new AverageStrategy().AggregateRange("EUR", "USD", D1, D1, candidates);

        Assert.HasCount(1, rates);
        Assert.AreEqual(1.09m, rates[0].Rate);
    }
}
