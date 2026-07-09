// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheContractTests{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

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
    protected static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

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

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that a stored fresh rate is returned on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredAndFresh_ShouldReturnRow()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, result);
        Assert.AreEqual(0.5000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that a stored rate older than the duration is filtered out on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredButStale_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        DateTimeOffset cachedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that freshness uses a strict less-than boundary: a rate cached exactly one duration ago is stale.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAgeEqualsDuration_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        DateTimeOffset cachedAt = DateTimeOffset.UtcNow - Duration;
        DateTimeOffset asOf = cachedAt + Duration;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, asOf);

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that storing the same date twice keeps the most recently cached value.
    /// </summary>
    [TestMethod]
    public void Store_WhenSameDateStoredTwice_ShouldKeepLatestCached()
    {
        TCache cache = CreateCache();
        DateTimeOffset older = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        DateTimeOffset newer = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);

        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.5000m, older) }, Duration, newer);
        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.6000m, newer) }, Duration, newer);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, newer);

        Assert.HasCount(1, result);
        Assert.AreEqual(0.6000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that storing fresh rows merges with, rather than replaces, existing fresh rows for other dates.
    /// </summary>
    [TestMethod]
    public void Store_WhenDifferentDates_ShouldMergeAndOrderByDate()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(2, result);
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
        DateTimeOffset stale = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, stale) }, Duration, stale);
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, result);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result[0].Date);
    }

    /// <summary>
    /// Verifies that storing an empty set leaves the cache unchanged.
    /// </summary>
    [TestMethod]
    public void Store_WhenEmpty_ShouldBeNoOp()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        cache.Store(Pair, Array.Empty<CachedExchangeRate>(), Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(1, result);
    }

    /// <summary>
    /// Verifies that rates stored under one pair are not returned for a different pair.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredUnderDifferentPair_ShouldNotLeak()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> other = cache.GetRates(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD), Duration, now);

        Assert.IsEmpty(other);
    }

    /// <summary>
    /// Verifies that a recorded coverage window is reported as covered, both for the exact window and for a sub-window
    /// wholly inside it.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenWindowRecorded_ShouldContainWindowAndSubWindow()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
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
        DateTimeOffset now = DateTimeOffset.UtcNow;
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
        DateTimeOffset recordedAt = DateTimeOffset.UtcNow;
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

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        DateRangeCoverage other = cache.GetCoverage(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD), Duration, now);

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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
        Assert.HasCount(1, cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that recording coverage after rates were stored preserves those rows, confirming the two halves of the
    /// per-pair state are written independently.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenRatesPreviouslyStored_ShouldPreserveEntries()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(1, rows);
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
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0m, now) }, Duration, now);

        Assert.IsEmpty(cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that a row carrying a default (unset) date is silently dropped on store and not returned on read.
    /// </summary>
    [TestMethod]
    public void Store_WhenRowHasDefaultDate_ShouldNotReturnRow()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(default, 0.5000m, now) }, Duration, now);

        Assert.IsEmpty(cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that a valid row stored alongside an invalid one is retained while the invalid row is dropped.
    /// </summary>
    [TestMethod]
    public void Store_WhenMixOfValidAndInvalidRows_ShouldRetainOnlyValid()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

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
        Assert.HasCount(1, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write reports success and persists both halves: the rows are returned and
    /// the whole window is reported as covered.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenRowsAndWindowSupplied_ShouldStoreBothHalves()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            new[]
            {
                new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now),
                new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now),
            },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 6),
            Duration,
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Stored, status);
        Assert.HasCount(2, cache.GetRates(Pair, Duration, now));
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 6)));
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write with no rows still records the whole window as covered, so a later
    /// range query of the window is a hit that returns an empty list rather than a miss.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenRowsEmpty_ShouldStillRecordCoverage()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            Array.Empty<CachedExchangeRate>(),
            new DateOnly(2023, 1, 7),
            new DateOnly(2023, 1, 8),
            Duration,
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Stored, status);
        Assert.IsEmpty(cache.GetRates(Pair, Duration, now));

        // The empty-but-fetched window is covered: a range query over it is a hit, distinguishable from a miss because
        // the coverage contains the window even though no row exists.
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 7), new DateOnly(2023, 1, 8)));
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write merges with, rather than replaces, previously stored rows and
    /// coverage for a different window.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenPriorStatePresent_ShouldPreserveOtherWindow()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.StoreFetchedRange(
            Pair,
            new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 3),
            Duration,
            now);

        cache.StoreFetchedRange(
            Pair,
            new[] { new CachedExchangeRate(new DateOnly(2023, 1, 10), 0.5200m, now) },
            new DateOnly(2023, 1, 10),
            new DateOnly(2023, 1, 10),
            Duration,
            now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(2, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.AreEqual(new DateOnly(2023, 1, 10), rows[1].Date);

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, now);
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3)));
        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write drops a semantically invalid row while still recording the window as
    /// covered, so the coverage and the rows stay consistent.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenRowInvalid_ShouldDropRowButRecordCoverage()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(
            Pair,
            new[]
            {
                new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now),
                new CachedExchangeRate(new DateOnly(2023, 1, 4), -0.1m, now),
            },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 4),
            Duration,
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Stored, status);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(1, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4)));
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write rejects an inverted window.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        TCache cache = CreateCache();

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.StoreFetchedRange(
                Pair,
                Array.Empty<CachedExchangeRate>(),
                new DateOnly(2023, 1, 10),
                new DateOnly(2023, 1, 3),
                Duration,
                DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that concurrent atomic fetched-range writes for the same pair, each for a distinct window, lose neither
    /// rows nor coverage.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public async Task StoreFetchedRange_WhenConcurrentForSamePair_ShouldNotLoseRowsOrCoverage()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        const int Windows = 16;

        IEnumerable<Task> writes = Enumerable.Range(0, Windows).Select(i => Task.Run(() =>
        {
            DateOnly date = new DateOnly(2023, 2, 1).AddDays(i);
            cache.StoreFetchedRange(
                Pair,
                new[] { new CachedExchangeRate(date, 0.5000m + (0.0001m * i), now) },
                date,
                date,
                Duration,
                now);
        }));

        await Task.WhenAll(writes);

        Assert.HasCount(Windows, cache.GetRates(Pair, Duration, now));

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, now);
        for (int i = 0; i < Windows; i++)
        {
            DateOnly date = new DateOnly(2023, 2, 1).AddDays(i);
            Assert.IsTrue(coverage.Contains(date, date), $"coverage missing for {date:O}");
        }
    }

    /// <summary>
    /// Verifies that a row stored with a non-null <see cref="CachedExchangeRate.ObservedAtUtc" /> — carrying an offset
    /// and sub-second precision — is read back with that upstream fetch instant intact, across every backend.
    /// </summary>
    [TestMethod]
    public void Store_WhenRowHasObservedAtUtc_ShouldRoundTripObservedAtUtc()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset observedAt = new DateTimeOffset(2023, 1, 3, 16, 0, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now, observedAt) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(1, rows);
        Assert.AreEqual(observedAt, rows[0].ObservedAtUtc);
        Assert.AreEqual(observedAt.Offset, rows[0].ObservedAtUtc!.Value.Offset);
    }

    /// <summary>
    /// Verifies that a row stored without an upstream fetch instant reads back with a <see langword="null" />
    /// <see cref="CachedExchangeRate.ObservedAtUtc" />, across every backend.
    /// </summary>
    [TestMethod]
    public void Store_WhenRowHasNoObservedAtUtc_ShouldReadBackNull()
    {
        TCache cache = CreateCache();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);
        Assert.HasCount(1, rows);
        Assert.IsNull(rows[0].ObservedAtUtc);
    }

    /// <summary>
    /// Verifies that re-storing the same date with a newer cache-write instant keeps the newer row's upstream fetch
    /// instant, proving <see cref="CachedExchangeRate.ObservedAtUtc" /> rides along through a merge-and-prune cycle.
    /// </summary>
    [TestMethod]
    public void Store_WhenSameDateRestoredWithNewerCachedAt_ShouldKeepLatestObservedAt()
    {
        TCache cache = CreateCache();
        DateTimeOffset older = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        DateTimeOffset newer = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);
        var observedOld = new DateTimeOffset(2023, 1, 3, 8, 0, 0, TimeSpan.Zero);
        var observedNew = new DateTimeOffset(2023, 1, 3, 16, 0, 0, TimeSpan.Zero);

        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.5000m, older, observedOld) }, Duration, newer);
        cache.Store(Pair, new[] { new CachedExchangeRate(date, 0.6000m, newer, observedNew) }, Duration, newer);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, newer);
        Assert.HasCount(1, rows);
        Assert.AreEqual(0.6000m, rows[0].Rate);
        Assert.AreEqual(observedNew, rows[0].ObservedAtUtc);
    }
}
