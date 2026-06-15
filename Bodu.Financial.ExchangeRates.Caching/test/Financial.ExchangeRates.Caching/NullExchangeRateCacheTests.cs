// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the no-op behaviour of <see cref="NullExchangeRateCache" />.
/// </summary>
[TestClass]
public sealed class NullExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that the factory binds the supplied provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderSupplied_ShouldBindProvider()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");

        Assert.AreEqual("Yahoo", cache.Provider);
    }

    /// <summary>
    /// Verifies that the factory rejects a blank provider.
    /// </summary>
    [TestMethod]
    public void Create_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NullExchangeRateCache.Create("  ");
        });

        Assert.AreEqual("provider", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a read always returns an empty result, even after a store.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAnythingStored_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.Store(pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, TimeSpan.FromHours(24), now);

        Assert.AreEqual(0, cache.GetRates(pair, TimeSpan.FromHours(24), now).Count);
    }

    /// <summary>
    /// Verifies that coverage is never reported, even after a window is recorded.
    /// </summary>
    [TestMethod]
    public void GetCoverage_WhenCoverageRecorded_ShouldReturnEmpty()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        cache.RecordCoverage(pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), TimeSpan.FromHours(24), now);

        Assert.IsTrue(cache.GetCoverage(pair, TimeSpan.FromHours(24), now).IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="NullExchangeRateCache.Store" /> rejects a <see langword="null" /> rates collection,
    /// enforcing the same argument contract as every other backend rather than silently ignoring it.
    /// </summary>
    [TestMethod]
    public void Store_WhenRatesNull_ShouldThrowArgumentNullException()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.Store(pair, null!, TimeSpan.FromHours(24), now);
        });

        Assert.AreEqual("rates", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NullExchangeRateCache.RecordCoverage" /> rejects an inverted window, enforcing the same
    /// argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.RecordCoverage(pair, new DateOnly(2023, 1, 10), new DateOnly(2023, 1, 3), TimeSpan.FromHours(24), DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NullExchangeRateCache.StoreFetchedRange" /> rejects a <see langword="null" /> rows
    /// collection, enforcing the same argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenRowsNull_ShouldThrowArgumentNullException()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.StoreFetchedRange(pair, null!, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), TimeSpan.FromHours(24), now);
        });

        Assert.AreEqual("rows", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NullExchangeRateCache.StoreFetchedRange" /> rejects an inverted window, enforcing the
    /// same argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.StoreFetchedRange(
                pair,
                Array.Empty<CachedExchangeRate>(),
                new DateOnly(2023, 1, 10),
                new DateOnly(2023, 1, 3),
                TimeSpan.FromHours(24),
                DateTimeOffset.UtcNow);
        });

        Assert.AreEqual("start", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an atomic fetched-range write stores nothing and reports the write as skipped.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenInvoked_ShouldReturnSkippedAndStoreNothing()
    {
        IExchangeRateCache cache = NullExchangeRateCache.Create("Yahoo");
        ExchangeRatePair pair = new("AUD", "USD");
        var now = DateTimeOffset.UtcNow;

        ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(
            pair,
            new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 10),
            TimeSpan.FromHours(24),
            now);

        Assert.AreEqual(ExchangeRateCacheWriteStatus.Skipped, status);
        Assert.AreEqual(0, cache.GetRates(pair, TimeSpan.FromHours(24), now).Count);
        Assert.IsTrue(cache.GetCoverage(pair, TimeSpan.FromHours(24), now).IsEmpty);
    }
}
