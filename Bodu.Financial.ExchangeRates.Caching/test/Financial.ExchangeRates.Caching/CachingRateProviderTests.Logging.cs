// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingRateProvider" /> emits cache-hit and cache-miss diagnostics through a
/// supplied logger and remains silent when no logger is supplied.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that a cache miss resolved from the inner source emits a miss-and-store record.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheMissAndLoggerSupplied_ShouldLogMissStored()
    {
        CapturingLogger logger = new();
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(inner, _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 4502, logger.Entries);
    }

    /// <summary>
    /// Verifies that a fresh cached rate served without consulting the inner source emits a cache-hit record.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheFreshAndLoggerSupplied_ShouldLogCacheHit()
    {
        CapturingLogger logger = new();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(InnerWith(), _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 4501, logger.Entries);
    }

    /// <summary>
    /// Verifies that a configured non-default <see cref="CachingRateOptions.CacheHitLogLevel" /> is honoured, so a
    /// cache hit is emitted at the configured level rather than the <see cref="LogLevel.Information" /> default.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheHitLogLevelConfigured_ShouldLogAtConfiguredLevel()
    {
        CapturingLogger logger = new();
        _options.CacheHitLogLevel = LogLevel.Information;
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = new(InnerWith(), _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 4501, logger.Entries);
        Assert.DoesNotContain(e => e.Level == LogLevel.Information && e.EventId.Id == 4501, logger.Entries);
    }

    /// <summary>
    /// Verifies that serving a fresh cached rate without a logger succeeds, confirming the default <c>null</c> logger
    /// path is a no-op.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenNoLoggerSupplied_ShouldServeWithoutLogging()
    {
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(InnerWith());

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }
}
