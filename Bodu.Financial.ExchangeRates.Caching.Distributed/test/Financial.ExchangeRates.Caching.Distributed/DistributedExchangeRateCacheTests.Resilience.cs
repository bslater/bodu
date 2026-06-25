// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheTests.Resilience.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies that <see cref="DistributedExchangeRateCache" /> degrades gracefully when the backing
/// <see cref="IDistributedCache" /> faults or holds a corrupt blob, surfacing an empty read or a skipped write rather
/// than an exception, as required by the <see cref="IExchangeRateCache" /> contract.
/// </summary>
public sealed partial class DistributedExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a read against a failing backing store returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBackingStoreFails_ShouldReturnEmpty()
    {
        DistributedExchangeRateCache cache = CreateCache(new FailingDistributedCache());

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Verifies that a coverage read against a failing backing store returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenBackingStoreFails_ShouldReturnEmpty()
    {
        DistributedExchangeRateCache cache = CreateCache(new FailingDistributedCache());

        DateRangeCoverage coverage = cache.GetCoverage(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsTrue(coverage.IsEmpty);
    }

    /// <summary>
    /// Verifies that a write against a failing backing store is a silent no-op rather than throwing.
    /// </summary>
    [TestMethod]
    public void Store_WhenBackingStoreFails_ShouldBeNoOp()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DistributedExchangeRateCache cache = CreateCache(new FailingDistributedCache());

        // The store must not throw, and a subsequent read (also against the failing store) must report nothing.
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.IsEmpty(cache.GetRates(Pair, Duration, now));
    }

    /// <summary>
    /// Verifies that recording coverage against a failing backing store is a silent no-op rather than throwing, while
    /// still validating its arguments.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenBackingStoreFails_ShouldBeNoOp()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DistributedExchangeRateCache cache = CreateCache(new FailingDistributedCache());

        cache.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        Assert.IsTrue(cache.GetCoverage(Pair, Duration, now).IsEmpty);
    }

    /// <summary>
    /// Verifies that argument validation still throws on a failing backing store, confirming graceful degradation does
    /// not swallow contract violations.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenBackingStoreFailsAndStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        DistributedExchangeRateCache cache = CreateCache(new FailingDistributedCache());

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.RecordCoverage(Pair, new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 3), Duration, DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a read against a backing store holding a corrupt (non-JSON) blob returns an empty result rather
    /// than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBlobIsCorrupt_ShouldReturnEmpty()
    {
        MemoryDistributedCache backingStore = CreateBackingStore();
        backingStore.Set("Test:AUDUSD", Encoding.UTF8.GetBytes("this is not json"));
        DistributedExchangeRateCache cache = CreateCache(backingStore);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(rows);
    }

    /// <summary>
    /// Verifies that a single malformed row inside an otherwise valid blob is skipped while the valid rows are still
    /// served, so one corrupt value never fails the whole read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBlobHasOneMalformedRow_ShouldSkipOnlyThatRow()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string cachedAt = now.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        MemoryDistributedCache backingStore = CreateBackingStore();

        // A blob whose first row carries an unparseable date and whose second row is well-formed.
        string json =
            $$"""
            {"rates":[{"date":"not-a-date","rate":"0.5","cachedAtUtc":"{{cachedAt}}"},{"date":"2023-01-03","rate":"0.6000","cachedAtUtc":"{{cachedAt}}"}],"coverage":[]}
            """;
        backingStore.Set("Test:AUDUSD", Encoding.UTF8.GetBytes(json));
        DistributedExchangeRateCache cache = CreateCache(backingStore);

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.AreEqual(0.6000m, rows[0].Rate);
    }
}
