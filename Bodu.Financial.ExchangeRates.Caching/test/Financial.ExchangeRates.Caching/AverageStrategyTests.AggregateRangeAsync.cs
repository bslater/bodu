// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AverageStrategyTests.AggregateRangeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AverageStrategyTests
{
    /// <summary>
    /// Verifies that the range overload averages only the dates present in every contributing candidate.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task AggregateRangeAsync_WhenDatesMisaligned_ShouldAverageOnlySharedDates()
    {
        IReadOnlyList<NamedDatedExchangeRateProvider> candidates = new[]
        {
            Named("A", ("AUD", "USD", new DateOnly(2024, 1, 1), 0.50m), ("AUD", "USD", new DateOnly(2024, 1, 2), 0.52m)),
            Named("B", ("AUD", "USD", new DateOnly(2024, 1, 2), 0.54m), ("AUD", "USD", new DateOnly(2024, 1, 3), 0.56m)),
        };

        IReadOnlyList<ExchangeRate> rates =
            await new AverageStrategy().AggregateRangeAsync("AUD", "USD", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3), candidates, default);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual(new DateOnly(2024, 1, 2), rates[0].Date);
        Assert.AreEqual(0.53m, rates[0].Rate);
    }
}
