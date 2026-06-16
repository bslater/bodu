// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by
/// <see cref="CachingExchangeRateProviderBase" /> while serving rates from the cache and delegating to wrapped sources.
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
    public static partial void RangeRefetched(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, int count, ExchangeRateCacheWriteStatus status);

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
    public static partial void RateProvenance(ILogger logger, LogLevel level, string source, string fromIsoCode, string toIsoCode, ExchangeRateOrigin origin, string? backend, TimeSpan? age);
}
