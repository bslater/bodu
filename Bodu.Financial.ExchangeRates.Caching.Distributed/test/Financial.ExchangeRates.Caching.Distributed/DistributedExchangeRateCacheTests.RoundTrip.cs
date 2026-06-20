// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies that <see cref="DistributedExchangeRateCache" /> round-trips decimals, dates, and instants losslessly
/// through its invariant ISO text JSON blob, reading back through a second instance over the same backing store.
/// </summary>
public sealed partial class DistributedExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a high-precision decimal rate, including its scale, round-trips exactly through the JSON blob.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateHasHighPrecision_ShouldRoundTripExactly()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        decimal rate = 1.234567890123456789m;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), rate, now) }, Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(rate, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that a decimal's trailing-zero scale is preserved through the JSON blob, so a stored <c>0.5000</c> is
    /// not read back as <c>0.5</c>.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateHasTrailingZeros_ShouldPreserveScale()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        decimal rate = 0.5000m;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), rate, now) }, Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        // decimal equality ignores scale, so compare the formatted scale explicitly.
        Assert.AreEqual("0.5000", row.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the observation date round-trips exactly through the JSON blob for dates far from the present.
    /// </summary>
    [TestMethod]
    public void Store_WhenDateIsFarFromPresent_ShouldRoundTripExactly()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var date = new DateOnly(1971, 2, 28);
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(date, 0.42m, now) }, TimeSpan.FromDays(36500), now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, TimeSpan.FromDays(36500), now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(date, rows[0].Date);
    }

    /// <summary>
    /// Verifies that the caching instant round-trips with its offset and sub-second precision intact through the JSON
    /// blob.
    /// </summary>
    [TestMethod]
    public void Store_WhenCachedInstantHasOffsetAndSubSeconds_ShouldRoundTripExactly()
    {
        DateTimeOffset cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);
        DateTimeOffset asOf = cachedAt + TimeSpan.FromMinutes(1);
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, cachedAt) }, Duration, asOf);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, asOf)[0];

        Assert.AreEqual(cachedAt, row.CachedAtUtc);
        Assert.AreEqual(cachedAt.Offset, row.CachedAtUtc.Offset);
    }

    /// <summary>
    /// Verifies that a row's upstream fetch instant round-trips with its offset and sub-second precision intact through
    /// the JSON blob, confirming the <c>observedAtUtc</c> property persists losslessly.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcHasOffsetAndSubSeconds_ShouldRoundTripThroughBlob()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset observedAt = new DateTimeOffset(2023, 1, 3, 16, 0, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now, observedAt) }, Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        Assert.AreEqual(observedAt, row.ObservedAtUtc);
        Assert.AreEqual(observedAt.Offset, row.ObservedAtUtc!.Value.Offset);
    }

    /// <summary>
    /// Verifies that a row stored without an upstream fetch instant reads back with a <see langword="null" />
    /// <see cref="CachedExchangeRate.ObservedAtUtc" /> through the JSON blob, so a missing instant survives as null.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcNull_ShouldReadBackNullThroughBlob()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        Assert.IsNull(row.ObservedAtUtc);
    }

    /// <summary>
    /// Verifies that a row carrying no upstream fetch instant omits the <c>observedAtUtc</c> property from the serialized
    /// blob, keeping legacy-shaped blobs minimal.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcNull_ShouldOmitPropertyFromBlob()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();
        DistributedExchangeRateCache cache = CreateCache(backingStore);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);

        string json = System.Text.Encoding.UTF8.GetString(backingStore.Get("Test:AUDUSD")!);
        Assert.IsFalse(json.Contains("observedAtUtc", StringComparison.OrdinalIgnoreCase), json);
    }

    /// <summary>
    /// Verifies that a recorded coverage window's dates and fetch instant round-trip exactly through the JSON blob.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenReadThroughSecondInstance_ShouldRoundTripWindowExactly()
    {
        var fetchedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 10);
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.RecordCoverage(Pair, start, end, Duration, fetchedAt);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, fetchedAt);

        Assert.IsTrue(coverage.Contains(start, end));
        Assert.IsFalse(coverage.Contains(start.AddDays(-1), end));
    }
}
