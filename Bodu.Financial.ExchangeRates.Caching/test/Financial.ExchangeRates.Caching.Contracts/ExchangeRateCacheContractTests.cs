// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Validates the <see cref="IExchangeRateCache" /> contract shared by every implementation: read-time freshness
/// filtering, write-time merge and prune, single-provider binding, and resilience to missing data.
/// </summary>
/// <typeparam name="TCache">The concrete cache under test.</typeparam>
public abstract class ExchangeRateCacheContractTests<TCache>
    where TCache : IExchangeRateCache
{
    /// <summary>
    /// The provider identifier the cache under test is bound to.
    /// </summary>
    protected const string Provider = "Test";

    /// <summary>
    /// The currency pair used by the contract tests.
    /// </summary>
    protected static readonly ExchangeRatePair Pair = new("AUD", "USD");

    /// <summary>
    /// The duration used by the contract tests.
    /// </summary>
    protected static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// Creates a fresh, empty cache bound to <see cref="Provider" /> for a single test.
    /// </summary>
    /// <returns>A new cache instance.</returns>
    protected abstract TCache CreateCache();

    /// <summary>
    /// Verifies that the cache reports the provider it was constructed for.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldBeBoundAtConstruction()
    {
        TCache cache = CreateCache();

        Assert.AreEqual(Provider, cache.Provider);
    }

    /// <summary>
    /// Verifies that a read against an empty cache returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenNothingStored_ShouldReturnEmpty()
    {
        TCache cache = CreateCache();

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that a stored fresh rate is returned on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredAndFresh_ShouldReturnRow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0.5000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that a stored rate older than the duration is filtered out on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredButStale_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        var cachedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that freshness uses a strict less-than boundary: a rate cached exactly one duration ago is stale.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAgeEqualsDuration_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        var cachedAt = DateTimeOffset.UtcNow - Duration;
        var asOf = cachedAt + Duration;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, asOf);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that storing the same date twice keeps the most recently cached value.
    /// </summary>
    [TestMethod]
    public void Store_WhenSameDateStoredTwice_ShouldKeepLatestCached()
    {
        TCache cache = CreateCache();
        var older = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var newer = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);

        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.5000m, older) }, Duration, newer);
        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.6000m, newer) }, Duration, newer);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, newer);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0.6000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that storing fresh rows merges with, rather than replaces, existing fresh rows for other dates.
    /// </summary>
    [TestMethod]
    public void Store_WhenDifferentDates_ShouldMergeAndOrderByDate()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result[0].Date);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result[1].Date);
    }

    /// <summary>
    /// Verifies that a write prunes previously stored rows that have since become stale.
    /// </summary>
    [TestMethod]
    public void Store_WhenExistingRowsStale_ShouldPruneOnWrite()
    {
        TCache cache = CreateCache();
        var stale = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        var now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, stale) }, Duration, stale);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result[0].Date);
    }

    /// <summary>
    /// Verifies that storing an empty set leaves the cache unchanged.
    /// </summary>
    [TestMethod]
    public void Store_WhenEmpty_ShouldBeNoOp()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        cache.Store(Pair, Array.Empty<CachedExchangeRate>(), Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that rates stored under one pair are not returned for a different pair.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredUnderDifferentPair_ShouldNotLeak()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> other = cache.GetRates(new ExchangeRatePair("EUR", "USD"), Duration, now);

        Assert.AreEqual(0, other.Count);
    }

    /// <summary>
    /// Verifies that a recorded coverage window is reported as covered, both for the exact window and for a sub-window
    /// wholly inside it.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenWindowRecorded_ShouldContainWindowAndSubWindow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, now);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 5), new DateOnly(2023, 1, 8)));
    }

    /// <summary>
    /// Verifies that a window straddling an unfetched gap between two recorded windows is not reported as covered, while
    /// each recorded window remains covered on its own.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenWindowStraddlesGap_ShouldNotContainStraddlingWindow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4), Duration, now);
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 8), new DateOnly(2023, 1, 10), Duration, now);

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, now);

        Assert.IsFalse(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4)));
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 8), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that a read against a pair with no recorded coverage returns an empty <see cref="DateRangeCoverage" />.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenNothingRecorded_ShouldReturnEmpty()
    {
        TCache cache = CreateCache();

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsTrue(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that a coverage window recorded at an instant is no longer reported as covered once one duration has
    /// elapsed, using the same strict less-than freshness boundary as cached rows.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenWindowExpired_ShouldNotBeCovered()
    {
        TCache cache = CreateCache();
        var recordedAt = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, recordedAt);

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, recordedAt + Duration);

        Assert.IsTrue(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="IExchangeRateCache.RecordCoverage" /> rejects an inverted window.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        TCache cache = CreateCache();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.RecordCoverage(Pair, new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 3), Duration, DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that coverage recorded under one pair is not reported for a different pair.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenRecordedUnderDifferentPair_ShouldNotLeak()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        DateRangeCoverage other = cache.GetCoverage(new ExchangeRatePair("EUR", "USD"), Duration, now);

        Assert.IsTrue(other.IsEmpty);
    }

    /// <summary>
    /// Verifies that storing rates after coverage was recorded preserves that coverage, confirming the two halves of the
    /// per-pair state are written independently.
    /// </summary>
    [TestMethod]
    public void Store_WhenCoveragePreviouslyRecorded_ShouldPreserveCoverage()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
        Assert.AreEqual(1, cache.GetRates(Pair, Duration, now).Count);
    }

    /// <summary>
    /// Verifies that recording coverage after rates were stored preserves those rows, confirming the two halves of the
    /// per-pair state are written independently.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenRatesPreviouslyStored_ShouldPreserveEntries()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(0.5000m, rows[0].Rate);
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that a row carrying a non-positive rate is silently dropped on store and not returned on read.
    /// </summary>
    [TestMethod]
    public void Store_WhenRowHasNonPositiveRate_ShouldNotReturnRow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0m, now) }, Duration, now);

        Assert.AreEqual(0, cache.GetRates(Pair, Duration, now).Count);
    }

    /// <summary>
    /// Verifies that a row carrying a default (unset) date is silently dropped on store and not returned on read.
    /// </summary>
    [TestMethod]
    public void Store_WhenRowHasDefaultDate_ShouldNotReturnRow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(default, 0.5000m, now) }, Duration, now);

        Assert.AreEqual(0, cache.GetRates(Pair, Duration, now).Count);
    }

    /// <summary>
    /// Verifies that a valid row stored alongside an invalid one is retained while the invalid row is dropped.
    /// </summary>
    [TestMethod]
    public void Store_WhenMixOfValidAndInvalidRows_ShouldRetainOnlyValid()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        cache.Store(
            Pair,
            new[]
            {
                new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now),
                new CachedExchangeRate(new DateOnly(2023, 1, 6), -0.1m, now),
            },
            Duration,
            now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
    }
}
