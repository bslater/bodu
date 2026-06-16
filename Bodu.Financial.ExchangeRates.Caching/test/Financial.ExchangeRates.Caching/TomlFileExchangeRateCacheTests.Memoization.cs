// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileExchangeRateCacheTests.Memoization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that <see cref="TomlFileExchangeRateCache" /> memoizes the parsed file per pair, serving the cached parse
/// while the file's last-write timestamp is unchanged and re-parsing once it moves.
/// </summary>
public sealed partial class TomlFileExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that a repeated read of a file whose last-write timestamp has not moved is served from the memoized
    /// parse: the file's content is rewritten underneath while its original timestamp is restored, and the cache still
    /// returns the originally parsed rows.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileTimestampUnchanged_ShouldServeMemoizedParse()
    {
        var now = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        TomlFileExchangeRateCache cache = CreateCache();
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.50m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> first = cache.GetRates(Pair, Duration, now);
        Assert.AreEqual(0.50m, first.Single().Rate);

        var path = cache.ResolveFilePath(Pair);
        var stamp = File.GetLastWriteTimeUtc(path);

        // Rewrite the file's content through a second instance, then restore the original timestamp so the first
        // instance's memo (keyed by that timestamp) is still considered current.
        TomlFileExchangeRateCache other = CreateCache();
        other.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.99m, now) }, Duration, now);
        File.SetLastWriteTimeUtc(path, stamp);

        IReadOnlyList<CachedExchangeRate> second = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(0.50m, second.Single().Rate, "an unchanged timestamp serves the memoized parse");
    }

    /// <summary>
    /// Verifies that a read re-parses the file once its last-write timestamp moves, so an external or cross-process
    /// write is observed rather than masked by the memo.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFileTimestampChanges_ShouldReParse()
    {
        var now = new DateTimeOffset(2023, 1, 4, 9, 15, 0, TimeSpan.Zero);
        TomlFileExchangeRateCache cache = CreateCache();
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.50m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> first = cache.GetRates(Pair, Duration, now);
        Assert.AreEqual(0.50m, first.Single().Rate);

        var path = cache.ResolveFilePath(Pair);

        // Rewrite the content through a second instance and move the timestamp forward so the memo is invalidated.
        TomlFileExchangeRateCache other = CreateCache();
        other.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.99m, now) }, Duration, now);
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(5));

        IReadOnlyList<CachedExchangeRate> second = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(0.99m, second.Single().Rate, "a changed timestamp forces a re-parse");
    }
}
