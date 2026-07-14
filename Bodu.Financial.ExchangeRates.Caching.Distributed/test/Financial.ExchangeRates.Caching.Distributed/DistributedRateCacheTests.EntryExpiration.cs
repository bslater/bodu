// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.EntryExpiration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheTests
{
    /// <summary>The fixed evaluation instant the expiration tests write at.</summary>
    private static readonly DateTimeOffset WriteInstant = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that with the default margin every stored blob carries a server-side lifetime of the caching
    /// duration plus the margin, so untouched keys self-evict server-side.
    /// </summary>
    [TestMethod]
    public void Store_WhenDefaultMargin_ShouldStampAbsoluteExpiration()
    {
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions { Provider = Provider });

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, WriteInstant) }, Duration, WriteInstant);

        Assert.HasCount(1, backing.Sets);
        Assert.AreEqual(
            Duration + TimeSpan.FromHours(1),
            backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow,
            "the blob's server-side lifetime must be duration + margin");
    }

    /// <summary>
    /// Verifies that an atomic range store stamps the same server-side lifetime onto the combined blob.
    /// </summary>
    [TestMethod]
    public void StoreFetchedRange_WhenDefaultMargin_ShouldStampAbsoluteExpiration()
    {
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions { Provider = Provider });
        var date = new DateOnly(2026, 6, 1);

        cache.StoreFetchedRange(Pair, new[] { new CachedRate(date, 0.5m, WriteInstant) }, date, date, Duration, WriteInstant);

        Assert.HasCount(1, backing.Sets);
        Assert.AreEqual(Duration + TimeSpan.FromHours(1), backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
    }

    /// <summary>
    /// Verifies that a custom margin is honoured in the stamped lifetime.
    /// </summary>
    [TestMethod]
    public void Store_WhenCustomMargin_ShouldStampAbsoluteExpirationWithThatMargin()
    {
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions
        {
            Provider = Provider,
            EntryExpirationMargin = TimeSpan.FromMinutes(5),
        });

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, WriteInstant) }, Duration, WriteInstant);

        Assert.AreEqual(Duration + TimeSpan.FromMinutes(5), backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> margin restores the pre-margin behaviour: the blob is written without
    /// any server-side expiration.
    /// </summary>
    [TestMethod]
    public void Store_WhenMarginDisabled_ShouldWriteWithoutServerSideExpiration()
    {
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions
        {
            Provider = Provider,
            EntryExpirationMargin = null,
        });

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, WriteInstant) }, Duration, WriteInstant);

        Assert.HasCount(1, backing.Sets);
        Assert.IsNull(backing.Sets[0].Options?.AbsoluteExpiration, "a disabled margin must not stamp an absolute expiration");
        Assert.IsNull(backing.Sets[0].Options?.AbsoluteExpirationRelativeToNow);
        Assert.IsNull(backing.Sets[0].Options?.SlidingExpiration);
    }

    /// <summary>
    /// Verifies that a blob written with the stamped expiration still round-trips through a read, so the expiration
    /// options do not disturb the payload itself.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBlobStampedWithExpiration_ShouldStillRoundTrip()
    {
        var backing = new RecordingDistributedCache();
        var cache = new DistributedRateCache(backing, new DistributedRateCacheOptions { Provider = Provider });

        cache.Store(Pair, new[] { new CachedRate(new DateOnly(2026, 6, 1), 0.5m, WriteInstant) }, Duration, WriteInstant);
        IReadOnlyList<CachedRate> rows = cache.GetRates(Pair, Duration, WriteInstant);

        Assert.HasCount(1, rows);
        Assert.AreEqual(0.5m, rows[0].Rate);
    }
}
