// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="AggregatingExchangeRateProvider" /> emits routing and aggregation diagnostics through a
/// supplied logger.
/// </summary>
public sealed partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a resolved lookup emits the aggregated diagnostic (event id 4511).
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenResolvedAndLoggerSupplied_ShouldLogAggregated()
    {
        CapturingLogger logger = new();
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA", ("USD", "AUD", D1, 1.5m)) }, options: null, timeProvider: null, logger: logger);

        agg.TryGetRate("USD", "AUD", D1, RateLookupOptions.Exact, out _);

        Assert.Contains(e => e.EventId.Id == 4511, logger.Entries);
    }

    /// <summary>
    /// Verifies that a lookup no child can satisfy emits the unresolved diagnostic (event id 4512).
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenUnresolvedAndLoggerSupplied_ShouldLogUnresolved()
    {
        CapturingLogger logger = new();
        AggregatingExchangeRateProvider agg = new(new[] { Named("RBA") }, options: null, timeProvider: null, logger: logger);

        agg.TryGetRate("USD", "AUD", D1, RateLookupOptions.Exact, out _);

        Assert.Contains(e => e.EventId.Id == 4512, logger.Entries);
    }
}
