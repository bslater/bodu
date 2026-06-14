// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProviderTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingDatedExchangeRateProvider" /> emits cache-hit and cache-miss diagnostics through a
/// supplied logger and remains silent when no logger is supplied.
/// </summary>
public sealed partial class CachingDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a cache miss resolved from the inner source emits a miss-and-store record.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheMissAndLoggerSupplied_ShouldLogMissStored()
    {
        CapturingLogger logger = new();
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = new(new[] { Source(Provider, inner) }, _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Trace && e.EventId.Id == 4502));
    }

    /// <summary>
    /// Verifies that a fresh cached rate served without consulting the inner source emits a cache-hit record.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheFreshAndLoggerSupplied_ShouldLogCacheHit()
    {
        CapturingLogger logger = new();
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = new(new[] { Source(Provider, InnerWith()) }, _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Trace && e.EventId.Id == 4501));
    }

    /// <summary>
    /// Verifies that serving a fresh cached rate without a logger succeeds, confirming the default <c>null</c> logger
    /// path is a no-op.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenNoLoggerSupplied_ShouldServeWithoutLogging()
    {
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(InnerWith());

        ExchangeRateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }
}
