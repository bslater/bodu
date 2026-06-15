// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.Provenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="CachingExchangeRateProvider" /> emits a provenance diagnostic at each serve point that
/// records whether the rate was resolved live or from the cache, the cache backend that served it, and the served
/// data's age.
/// </summary>
public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// The runtime identity of the in-memory cache the tests run against, as reported by the provenance message.
    /// </summary>
    private const string Backend = nameof(InMemoryExchangeRateCache);

    /// <summary>
    /// The event id of the provenance diagnostic.
    /// </summary>
    private const int ProvenanceEventId = 4505;

    /// <summary>
    /// Verifies that a single-date cache hit emits the provenance event with a <c>Cache</c> origin, the in-memory cache
    /// backend, and an age equal to the elapsed time since the served row was cached.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheHit_ShouldLogProvenanceWithCacheOriginAndAge()
    {
        CapturingLogger logger = new();
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = new(InnerWith(), _cache, _options, _clock, logger);

        _clock.Advance(TimeSpan.FromHours(1));
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        (LogLevel Level, EventId EventId, string Message) entry = logger.Entries.Single(e => e.EventId.Id == ProvenanceEventId);
        Assert.AreEqual(LogLevel.Debug, entry.Level);
        Assert.IsTrue(entry.Message.Contains("from Cache", StringComparison.Ordinal), entry.Message);
        Assert.IsTrue(entry.Message.Contains($"backend '{Backend}'", StringComparison.Ordinal), entry.Message);
        Assert.IsTrue(entry.Message.Contains("age 01:00:00", StringComparison.Ordinal), entry.Message);
    }

    /// <summary>
    /// Verifies that a single-date cache miss resolved from the inner source emits the provenance event with a
    /// <c>Live</c> origin and no age.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheMiss_ShouldLogProvenanceWithLiveOriginAndNoAge()
    {
        CapturingLogger logger = new();
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = new(inner, _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        (LogLevel Level, EventId EventId, string Message) entry = logger.Entries.Single(e => e.EventId.Id == ProvenanceEventId);
        Assert.AreEqual(LogLevel.Debug, entry.Level);
        Assert.IsTrue(entry.Message.Contains("from Live", StringComparison.Ordinal), entry.Message);
        Assert.IsTrue(entry.Message.Contains("age )", StringComparison.Ordinal), entry.Message);
    }

    /// <summary>
    /// Verifies that a range lookup served entirely from the cache emits the provenance event with a <c>Cache</c>
    /// origin and the in-memory cache backend.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenCacheHit_ShouldLogProvenanceWithCacheOrigin()
    {
        CapturingLogger logger = new();
        ExchangeRatePair pair = new("AUD", "USD");
        SeedCache(pair, (new DateOnly(2023, 1, 3), 0.5m), (new DateOnly(2023, 1, 6), 0.51m));
        SeedCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));
        CachingExchangeRateProvider sut = new(InnerWith(), _cache, _options, _clock, logger);

        _clock.Advance(TimeSpan.FromHours(1));
        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        (LogLevel Level, EventId EventId, string Message) entry = logger.Entries.Single(e => e.EventId.Id == ProvenanceEventId);
        Assert.AreEqual(LogLevel.Debug, entry.Level);
        Assert.IsTrue(entry.Message.Contains("from Cache", StringComparison.Ordinal), entry.Message);
        Assert.IsTrue(entry.Message.Contains($"backend '{Backend}'", StringComparison.Ordinal), entry.Message);
    }

    /// <summary>
    /// Verifies that a range lookup refetched from the inner source emits the provenance event with a <c>Live</c>
    /// origin and no age.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRefetch_ShouldLogProvenanceWithLiveOrigin()
    {
        CapturingLogger logger = new();
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m),
            ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingExchangeRateProvider sut = new(inner, _cache, _options, _clock, logger);

        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6));

        (LogLevel Level, EventId EventId, string Message) entry = logger.Entries.Single(e => e.EventId.Id == ProvenanceEventId);
        Assert.AreEqual(LogLevel.Debug, entry.Level);
        Assert.IsTrue(entry.Message.Contains("from Live", StringComparison.Ordinal), entry.Message);
        Assert.IsTrue(entry.Message.Contains("age )", StringComparison.Ordinal), entry.Message);
    }

    /// <summary>
    /// Verifies that setting <see cref="CachingExchangeRateOptions.RateProvenanceLogLevel" /> to
    /// <see cref="LogLevel.None" /> suppresses the provenance event while the cache-hit event still emits at its own
    /// level.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenRateProvenanceLogLevelNone_ShouldSuppressProvenanceButStillLogHit()
    {
        CapturingLogger logger = new();
        _options.RateProvenanceLogLevel = LogLevel.None;
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = new(InnerWith(), _cache, _options, _clock, logger);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        Assert.IsFalse(logger.Entries.Any(e => e.EventId.Id == ProvenanceEventId));
        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Trace && e.EventId.Id == 4501));
    }
}
