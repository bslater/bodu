// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRateCacheTests.StoreFetchedRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class NullRateCacheTests
{
    /// <summary>
    /// Verifies that <see cref="NullRateCache.StoreFetchedRange" /> rejects a <see langword="null" /> rows
    /// collection, enforcing the same argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenRowsNull_ShouldThrowArgumentNullException()
    {
        IRateCache cache = NullRateCache.Create("Yahoo");
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.StoreFetchedRange(pair, null!, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), TimeSpan.FromHours(24), now);
        });

        Assert.AreEqual("rows", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NullRateCache.StoreFetchedRange" /> rejects an inverted window, enforcing the
    /// same argument contract as every other backend.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenStartAfterEnd_ShouldThrowArgumentOutOfRangeException()
    {
        IRateCache cache = NullRateCache.Create("Yahoo");
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            cache.StoreFetchedRange(
                pair,
                Array.Empty<CachedRate>(),
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
        IRateCache cache = NullRateCache.Create("Yahoo");
        CurrencyPair pair = new(CurrencyCode.AUD, CurrencyCode.USD);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        RateCacheWriteStatus status = cache.StoreFetchedRange(
            pair,
            new[] { new CachedRate(new DateOnly(2023, 1, 3), 0.5m, now) },
            new DateOnly(2023, 1, 3),
            new DateOnly(2023, 1, 10),
            TimeSpan.FromHours(24),
            now);

        Assert.AreEqual(RateCacheWriteStatus.Skipped, status);
        Assert.IsEmpty(cache.GetRates(pair, TimeSpan.FromHours(24), now));
        Assert.IsTrue(cache.GetCoverage(pair, TimeSpan.FromHours(24), now).IsEmpty);
    }
}
