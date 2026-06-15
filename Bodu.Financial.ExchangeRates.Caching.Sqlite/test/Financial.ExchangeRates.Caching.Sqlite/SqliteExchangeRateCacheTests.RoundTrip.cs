// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteExchangeRateCache" /> round-trips decimals and dates losslessly through its invariant
/// ISO text storage, including reads from a freshly reopened instance.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a high-precision decimal rate, including its scale, round-trips exactly across a reopen.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateHasHighPrecision_ShouldRoundTripExactly()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();
        var rate = 1.234567890123456789m;

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), rate, now) }, Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(rate, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that a decimal's trailing-zero scale is preserved across a reopen, so a stored <c>0.5000</c> is not read
    /// back as <c>0.5</c>.
    /// </summary>
    [TestMethod]
    public void Store_WhenRateHasTrailingZeros_ShouldPreserveScale()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();
        var rate = 0.5000m;

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), rate, now) }, Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        // decimal equality ignores scale, so compare the formatted scale explicitly.
        Assert.AreEqual("0.5000", row.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the observation date round-trips exactly across a reopen for dates far from the present.
    /// </summary>
    [TestMethod]
    public void Store_WhenDateIsFarFromPresent_ShouldRoundTripExactly()
    {
        var now = DateTimeOffset.UtcNow;
        var path = NewDatabasePath();
        var date = new DateOnly(1971, 2, 28);

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(date, 0.42m, now) }, TimeSpan.FromDays(36500), now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, TimeSpan.FromDays(36500), now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(date, rows[0].Date);
    }

    /// <summary>
    /// Verifies that the caching instant round-trips with its offset and sub-second precision intact across a reopen.
    /// </summary>
    [TestMethod]
    public void Store_WhenCachedInstantHasOffsetAndSubSeconds_ShouldRoundTripExactly()
    {
        var path = NewDatabasePath();
        var cachedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);
        var asOf = cachedAt + TimeSpan.FromMinutes(1);

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, cachedAt) }, Duration, asOf);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, asOf)[0];

        Assert.AreEqual(cachedAt, row.CachedAtUtc);
        Assert.AreEqual(cachedAt.Offset, row.CachedAtUtc.Offset);
    }

    /// <summary>
    /// Verifies that a row's upstream fetch instant round-trips with its offset and sub-second precision intact across a
    /// writer-dispose and reader-reopen, confirming the <c>observed_at</c> column persists losslessly.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcHasOffsetAndSubSeconds_ShouldRoundTripAcrossReopen()
    {
        var path = NewDatabasePath();
        var now = DateTimeOffset.UtcNow;
        var observedAt = new DateTimeOffset(2023, 1, 3, 16, 0, 30, 123, TimeSpan.FromHours(10)).AddTicks(4567);

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now, observedAt) }, Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        Assert.AreEqual(observedAt, row.ObservedAtUtc);
        Assert.AreEqual(observedAt.Offset, row.ObservedAtUtc!.Value.Offset);
    }

    /// <summary>
    /// Verifies that a row stored without an upstream fetch instant reads back with a <see langword="null" />
    /// <see cref="CachedExchangeRate.ObservedAtUtc" /> across a reopen, so a missing instant survives as null.
    /// </summary>
    [TestMethod]
    public void Store_WhenObservedAtUtcNull_ShouldReadBackNullAcrossReopen()
    {
        var path = NewDatabasePath();
        var now = DateTimeOffset.UtcNow;

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5m, now) }, Duration, now);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        CachedExchangeRate row = reader.GetRates(Pair, Duration, now)[0];

        Assert.IsNull(row.ObservedAtUtc);
    }

    /// <summary>
    /// Verifies that a recorded coverage window's dates and fetch instant round-trip exactly across a reopen.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenReopened_ShouldRoundTripWindowExactly()
    {
        var fetchedAt = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        var path = NewDatabasePath();
        var start = new DateOnly(2023, 1, 3);
        var end = new DateOnly(2023, 1, 10);

        SqliteExchangeRateCache writer = CreateFileCache(path);
        writer.RecordCoverage(Pair, start, end, Duration, fetchedAt);
        writer.Dispose();

        SqliteExchangeRateCache reader = CreateFileCache(path);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, fetchedAt);

        Assert.IsTrue(coverage.Contains(start, end));
        Assert.IsFalse(coverage.Contains(start.AddDays(-1), end));
    }
}
