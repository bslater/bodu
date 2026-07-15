// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="CachingRateProviderBase" />
/// while serving rates from the cache and delegating to wrapped sources.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a single-date lookup was served from the cache.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name the fresh rows are cached under.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    [LoggerMessage(EventId = 4501, Message = "Served {fromIsoCode}->{toIsoCode} on {date} from cache for source '{source}'")]
    public static partial void CacheHit(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly date);

    /// <summary>
    /// Logs that a single-date lookup missed the cache and was resolved from a wrapped source and then cached.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name consulted on the miss.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    [LoggerMessage(EventId = 4502, Message = "Cache miss for {fromIsoCode}->{toIsoCode} on {date}; resolved from source '{source}' and cached")]
    public static partial void CacheMissStored(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly date);

    /// <summary>
    /// Logs that a range lookup was served entirely from the cache.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name the fresh rows are cached under.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    [LoggerMessage(EventId = 4503, Message = "Served {fromIsoCode}->{toIsoCode} range from cache for source '{source}'")]
    public static partial void RangeCacheHit(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode);

    /// <summary>
    /// Logs that a range lookup was refetched from a wrapped source and the atomic rows-and-coverage write attempted,
    /// reporting whether that write was persisted.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name consulted on the miss.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="count">The number of rates refetched.</param>
    /// <param name="status">The outcome of the atomic rows-and-coverage cache write.</param>
    [LoggerMessage(EventId = 4504, Message = "Refetched {count} {fromIsoCode}->{toIsoCode} rate(s) from source '{source}'; cache write {status}")]
    public static partial void RangeRefetched(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, int count, RateCacheWriteStatus status);

    /// <summary>
    /// Logs the provenance of a served rate, recording whether it came from a wrapped source or the cache, the cache
    /// backend that served it, and — for a cache serve — the age of the served data.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name the served rows are cached under.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="origin">Whether the rate was resolved live from a source or served from the cache.</param>
    /// <param name="backend">
    /// The runtime identity of the cache backend that served the request, or <see langword="null" /> for a rate
    /// resolved directly by the inner source.
    /// </param>
    /// <param name="age">
    /// The age of the served data, or <see langword="null" /> for a live serve or when no row backs the serve.
    /// </param>
    [LoggerMessage(EventId = 4505, Message = "Resolved {fromIsoCode}->{toIsoCode} for source '{source}' from {origin} (backend '{backend}', age {age})")]
    public static partial void RateProvenance(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, RateOrigin origin, string? backend, TimeSpan? age);

    /// <summary>
    /// Logs that a single-date lookup was not delegated to the wrapped source because the requested date lies outside
    /// the source's advertised history.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name whose advertised history caused the skip.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    /// <param name="earliest">The earliest date the source advertises it can serve.</param>
    [LoggerMessage(EventId = 4506, Message = "Skipped source '{source}' for {fromIsoCode}->{toIsoCode} on {date}: outside advertised history (earliest available {earliest})")]
    public static partial void SkippedDateOutsideHistory(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly date, DateOnly earliest);

    /// <summary>
    /// Logs that a range fetch was skipped entirely because no part of the requested window lies inside the wrapped
    /// source's advertised history, with the whole window recorded as covered-with-no-rows instead.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name whose advertised history caused the skip.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive first date of the requested window.</param>
    /// <param name="endDate">The inclusive last date of the requested window.</param>
    /// <param name="earliest">The earliest date the source advertises it can serve.</param>
    /// <param name="status">The outcome of the empty rows-and-coverage cache write.</param>
    [LoggerMessage(EventId = 4507, Message = "Skipped source '{source}' fetch for {fromIsoCode}->{toIsoCode} {startDate}..{endDate}: outside advertised history (earliest available {earliest}); recorded empty coverage, cache write {status}")]
    public static partial void SkippedRangeOutsideHistory(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, DateOnly earliest, RateCacheWriteStatus status);

    /// <summary>
    /// Logs that a range fetch was clamped to start at the wrapped source's advertised earliest date rather than the
    /// requested start, with the whole requested window still recorded as covered.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name whose advertised history caused the clamp.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive first date of the requested window.</param>
    /// <param name="fetchStart">The advertised earliest date the fetch was raised to.</param>
    [LoggerMessage(EventId = 4508, Message = "Clamped {fromIsoCode}->{toIsoCode} range fetch from source '{source}': requested start {startDate} raised to advertised earliest {fetchStart}")]
    public static partial void ClampedRangeToHistory(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly fetchStart);

    /// <summary>
    /// Logs that an aged cache hit scheduled a background refresh of the served window, so the data is refetched
    /// before it expires.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="source">The source name the refreshed rows are cached under.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive first date of the window being refreshed.</param>
    /// <param name="endDate">The inclusive last date of the window being refreshed.</param>
    [LoggerMessage(EventId = 4516, Message = "Scheduled refresh-ahead of {fromIsoCode}->{toIsoCode} {startDate}..{endDate} for source '{source}' after an aged cache hit")]
    public static partial void RefreshAheadScheduled(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Logs that a background refresh-ahead attempt failed and was swallowed; the hit that triggered it was already
    /// served, and the next aged hit schedules a fresh attempt.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="source">The source name the refresh targeted.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="startDate">The inclusive first date of the window whose refresh failed.</param>
    /// <param name="endDate">The inclusive last date of the window whose refresh failed.</param>
    /// <param name="exception">The swallowed refresh exception.</param>
    [LoggerMessage(EventId = 4517, Level = LogLevel.Warning, Message = "Refresh-ahead of {fromIsoCode}->{toIsoCode} {startDate}..{endDate} from source '{source}' failed and was swallowed; the next aged hit will retry")]
    public static partial void RefreshAheadFailed(ILogger logger, string source, string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, Exception exception);

    /// <summary>
    /// Logs that one pair's fetch failed during a warm-up and was swallowed, so the remaining pairs still warm.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="source">The source name the warm-up targeted.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="exception">The swallowed fetch exception.</param>
    [LoggerMessage(EventId = 4518, Level = LogLevel.Warning, Message = "Warm-up of {fromIsoCode}->{toIsoCode} from source '{source}' failed and was swallowed; remaining pairs continue")]
    public static partial void WarmPairFailed(ILogger logger, string source, string fromIsoCode, string toIsoCode, Exception exception);
}
