// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheTests
{
    /// <summary>
    /// Verifies that a swallowed backing-store read failure is logged once at <see cref="LogLevel.Warning" />
    /// carrying the originating exception and the cache's reserved event id, so the degradation is not silent.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBackingStoreFailsAndLoggerSupplied_ShouldLogWarningWithException()
    {
        var logger = new CapturingLogger();
        var cache = new DistributedRateCache(
            new FailingDistributedCache(),
            new DistributedRateCacheOptions { Provider = Provider },
            timeProvider: null,
            logger: logger);

        _ = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> warnings =
            logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual(4530, warnings[0].EventId.Id, "the degradation uses the cache's reserved event id");
        Assert.IsNotNull(warnings[0].Exception, "the swallowed exception is attached to the warning");
        Assert.IsInstanceOfType<InvalidOperationException>(warnings[0].Exception);
        Assert.IsTrue(warnings[0].Message.Contains(Provider, StringComparison.Ordinal), "the warning names the degraded provider");
    }

    /// <summary>
    /// Verifies that repeated swallowed failures inside the rate-limit window emit only one warning, so a sustained
    /// backing-store outage does not flood the log.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBackingStoreFailsRepeatedly_ShouldRateLimitWarnings()
    {
        var logger = new CapturingLogger();
        var cache = new DistributedRateCache(
            new FailingDistributedCache(),
            new DistributedRateCacheOptions { Provider = Provider },
            timeProvider: null,
            logger: logger);

        for (int i = 0; i < 5; i++)
        {
            _ = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);
        }

        Assert.HasCount(1, logger.Entries.Where(e => e.Level == LogLevel.Warning).ToList(), "failures inside the cooldown window are suppressed");
    }

    /// <summary>
    /// Verifies that, with no logger supplied, a swallowed backing-store failure still degrades gracefully without
    /// throwing, so the logging path is null-safe.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBackingStoreFailsAndNoLogger_ShouldNotThrow()
    {
        var cache = new DistributedRateCache(new FailingDistributedCache(), new DistributedRateCacheOptions { Provider = Provider });

        IReadOnlyList<CachedRate> rates = cache.GetRates(Pair, Duration, DateTimeOffset.UtcNow);

        Assert.IsEmpty(rates);
    }
}
