// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// Verifies that <see cref="SqliteExchangeRateCache" /> reports its best-effort storage degradation through the supplied
/// logger at <see cref="LogLevel.Warning" />, rate-limited so a sustained outage is visible without flooding the log.
/// </summary>
public sealed partial class SqliteExchangeRateCacheTests
{
    /// <summary>
    /// The instant the deterministic logging tests start their clock at.
    /// </summary>
    private static readonly DateTimeOffset LogClockStart = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a swallowed storage failure is logged once at <see cref="LogLevel.Warning" /> carrying the
    /// originating exception and the cache's event id, so the degradation is not silent.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseCorruptAndLoggerSupplied_ShouldLogWarningWithException()
    {
        string path = NewCorruptDatabaseFile();
        var logger = new CapturingLogger();

        var cache = new SqliteExchangeRateCache(
            new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path },
            timeProvider: null,
            logger: logger);
        _caches.Add(cache);

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings = Warnings(logger);

        Assert.HasCount(1, warnings);
        Assert.AreEqual(4520, warnings[0].EventId.Id, "the degradation uses the cache's reserved event id");
        Assert.IsNotNull(warnings[0].Exception, "the swallowed exception is attached to the warning");
        Assert.IsInstanceOfType<SqliteException>(warnings[0].Exception);
        Assert.IsTrue(warnings[0].Message.Contains(Provider, StringComparison.Ordinal), "the warning names the degraded provider");
    }

    /// <summary>
    /// Verifies that, with no logger supplied, a swallowed storage failure still degrades gracefully without throwing,
    /// so the logging path is null-safe.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenDatabaseCorruptAndNoLogger_ShouldNotThrow()
    {
        string path = NewCorruptDatabaseFile();

        var cache = new SqliteExchangeRateCache(new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path });
        _caches.Add(cache);

        _ = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Verifies that repeated failures inside the cooldown window are rate-limited to the single warning already emitted,
    /// so a sustained outage does not flood the log.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenRepeatedFailuresWithinCooldown_ShouldLogSingleWarning()
    {
        string path = NewCorruptDatabaseFile();
        var logger = new CapturingLogger();
        var clock = new MutableTimeProvider(LogClockStart);

        var cache = new SqliteExchangeRateCache(
            new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path },
            clock,
            logger);
        _caches.Add(cache);

        // Many further reads at the same instant all fall inside the cooldown opened by the construction warning.
        for (int i = 0; i < 5; i++)
            _ = cache.GetRates(Pair, Duration, clock.GetUtcNow());

        Assert.HasCount(1, Warnings(logger), "failures inside the cooldown window are suppressed, not logged");
    }

    /// <summary>
    /// Verifies that once the cooldown elapses a further failure logs again, reporting the count of failures suppressed
    /// since the previous warning and naming the operation that failed.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenFailuresSpanCooldown_ShouldLogAgainWithSuppressedCount()
    {
        string path = NewCorruptDatabaseFile();
        var logger = new CapturingLogger();
        var clock = new MutableTimeProvider(LogClockStart);

        var cache = new SqliteExchangeRateCache(
            new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path },
            clock,
            logger);
        _caches.Add(cache);

        // Three suppressed reads inside the cooldown opened by the construction warning.
        for (int i = 0; i < 3; i++)
            _ = cache.GetRates(Pair, Duration, clock.GetUtcNow());

        // Past the cooldown, the next failure logs again.
        clock.Advance(TimeSpan.FromMinutes(2));
        _ = cache.GetRates(Pair, Duration, clock.GetUtcNow());

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings = Warnings(logger);

        Assert.HasCount(2, warnings);
        Assert.IsTrue(warnings[1].Message.Contains("3 further failure(s)", StringComparison.Ordinal), "the second warning reports the three suppressed reads");
        Assert.IsTrue(warnings[1].Message.Contains("storage read", StringComparison.Ordinal), "the second warning names the read operation");
    }

    /// <summary>
    /// Verifies that a write operation against a corrupt database also surfaces the degradation as a warning once the
    /// cooldown has elapsed, so the write path is not silent either.
    /// </summary>
    [TestMethod]
    public void Store_WhenDatabaseCorrupt_ShouldLogWarning()
    {
        string path = NewCorruptDatabaseFile();
        var logger = new CapturingLogger();
        var clock = new MutableTimeProvider(LogClockStart);

        var cache = new SqliteExchangeRateCache(
            new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path },
            clock,
            logger);
        _caches.Add(cache);

        // Advance past the construction warning's cooldown so the store's own swallow logs rather than being suppressed.
        clock.Advance(TimeSpan.FromMinutes(2));
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, clock.GetUtcNow()) }, Duration, clock.GetUtcNow());

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings = Warnings(logger);

        Assert.HasCount(2, warnings, "the store operation surfaces a second degradation warning after the cooldown");
        Assert.AreEqual(4520, warnings[1].EventId.Id);
    }

    /// <summary>
    /// Verifies that, with <see cref="ExchangeRateCacheOptions.ThrowOnStorageFailure" /> set, a storage failure
    /// propagates as an exception and no degradation warning is logged, since nothing is swallowed.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDatabaseCorruptAndThrowOnStorageFailure_ShouldThrowAndNotLog()
    {
        string path = NewCorruptDatabaseFile();
        var logger = new CapturingLogger();

        _ = Assert.ThrowsExactly<SqliteException>(() =>
        {
            _ = new SqliteExchangeRateCache(
                new SqliteExchangeRateCacheOptions { Provider = Provider, DatabaseFilePath = path, ThrowOnStorageFailure = true },
                timeProvider: null,
                logger: logger);
        });

        Assert.IsEmpty(Warnings(logger), "a strict cache propagates rather than swallowing, so nothing is logged");
    }

    /// <summary>
    /// Returns the warning-level entries captured by the supplied logger, in order.
    /// </summary>
    /// <param name="logger">The capturing logger to filter.</param>
    /// <returns>The warning entries.</returns>
    private static List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Warnings(CapturingLogger logger)
    {
        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings = new();
        foreach ((LogLevel Level, EventId EventId, string Message, Exception? Exception) entry in logger.Entries)
        {
            if (entry.Level == LogLevel.Warning)
                warnings.Add(entry);
        }

        return warnings;
    }
}
