// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by the notable-date caching layer: the
/// per-lookup cache hit and miss diagnostics of <see cref="CachingNotableDateService" /> and the best-effort
/// storage-failure and corrupt-file warnings of the file-backed caches.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that a computed year was served from the cache.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level the diagnostic is logged at.</param>
    /// <param name="territory">The territory the year was resolved for.</param>
    /// <param name="year">The civil year served.</param>
    /// <param name="ageSeconds">The age, in seconds, of the served entry at the lookup instant.</param>
    [LoggerMessage(
        EventId = 4601,
        Message = "Notable-date cache hit for territory '{territory}' year {year}; served entry aged {ageSeconds:F0}s")]
    public static partial void CacheHit(ILogger logger, LogLevel level, string territory, int year, double ageSeconds);

    /// <summary>
    /// Logs that a year was recomputed on a cache miss and written back to the cache.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level the diagnostic is logged at.</param>
    /// <param name="territory">The territory the year was resolved for.</param>
    /// <param name="year">The civil year recomputed.</param>
    /// <param name="occurrences">The number of occurrences the recomputed year yielded.</param>
    [LoggerMessage(
        EventId = 4602,
        Message = "Notable-date cache miss for territory '{territory}' year {year}; recomputed {occurrences} occurrence(s) and cached")]
    public static partial void CacheMiss(ILogger logger, LogLevel level, string territory, int year, int occurrences);

    /// <summary>
    /// Logs that a file-cache storage operation failed and was swallowed under the cache's best-effort contract, with
    /// the count of similar failures suppressed since the previous warning so a sustained outage does not flood the
    /// log.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="suppressedSinceLastWarning">
    /// The number of similar swallowed failures since the previous warning that were rate-limited away.
    /// </param>
    /// <param name="exception">The swallowed storage exception.</param>
    [LoggerMessage(
        EventId = 4603,
        Level = LogLevel.Warning,
        Message = "File notable-date cache degraded: a storage {operation} failed and was swallowed as best-effort; {suppressedSinceLastWarning} further failure(s) suppressed since the previous warning")]
    public static partial void FileStorageFailureSwallowed(ILogger logger, string operation, int suppressedSinceLastWarning, Exception exception);

    /// <summary>
    /// Logs that a cache file could not be deserialized and was treated as empty, so a corrupt or incompatible file
    /// surfaces to operators instead of silently reading as a miss until it is rewritten.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="path">The full path of the corrupt cache file.</param>
    /// <param name="exception">The swallowed deserialization exception.</param>
    [LoggerMessage(
        EventId = 4604,
        Level = LogLevel.Warning,
        Message = "File notable-date cache treated corrupt cache file '{path}' as empty; it will be repaired by the next successful write")]
    public static partial void CacheFileCorrupt(ILogger logger, string path, Exception exception);

    /// <summary>
    /// Logs that a year was recomputed in the background by refresh-ahead after an aged cache hit, so the entry was
    /// renewed before it expired.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level the diagnostic is logged at.</param>
    /// <param name="territory">The territory the year was recomputed for.</param>
    /// <param name="year">The civil year recomputed.</param>
    /// <param name="occurrences">The number of occurrences the recomputed year yielded.</param>
    [LoggerMessage(
        EventId = 4605,
        Message = "Refresh-ahead recomputed notable-date year {year} for territory '{territory}' after an aged cache hit; cached {occurrences} occurrence(s)")]
    public static partial void RefreshAheadRecomputed(ILogger logger, LogLevel level, string territory, int year, int occurrences);

    /// <summary>
    /// Logs that a background refresh-ahead recompute failed and was swallowed; the hit that triggered it was already
    /// served, and the next aged hit schedules a fresh attempt.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="territory">The territory whose recompute failed.</param>
    /// <param name="year">The civil year whose recompute failed.</param>
    /// <param name="exception">The swallowed recompute exception.</param>
    [LoggerMessage(
        EventId = 4606,
        Level = LogLevel.Warning,
        Message = "Refresh-ahead recompute of notable-date year {year} for territory '{territory}' failed and was swallowed; the next aged hit will retry")]
    public static partial void RefreshAheadFailed(ILogger logger, string territory, int year, Exception exception);

    /// <summary>
    /// Logs that one territory's resolution failed during a warm-up and was swallowed, so the remaining territories
    /// still warm.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="territory">The territory whose warm-up failed.</param>
    /// <param name="exception">The swallowed resolution exception.</param>
    [LoggerMessage(
        EventId = 4607,
        Level = LogLevel.Warning,
        Message = "Warm-up of notable dates for territory '{territory}' failed and was swallowed; remaining territories continue")]
    public static partial void WarmTerritoryFailed(ILogger logger, string territory, Exception exception);
}
