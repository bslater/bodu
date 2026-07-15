// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.Warmup.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the source-generated logging messages emitted by <see cref="NotableDateCacheWarmupService" /> around the
/// startup cache warm-up.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that the startup warm-up began, with the territory count and the resolved year span.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="territoryCount">The number of territories being warmed.</param>
    /// <param name="firstYear">The inclusive first civil year of the warmed span.</param>
    /// <param name="lastYear">The inclusive last civil year of the warmed span.</param>
    [LoggerMessage(EventId = 4622, Level = LogLevel.Information, Message = "Notable-date cache warm-up started: {territoryCount} territory(ies) over years {firstYear}..{lastYear}")]
    public static partial void WarmupStarted(ILogger logger, int territoryCount, int firstYear, int lastYear);

    /// <summary>
    /// Logs that the startup warm-up completed, with the count of territories warmed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="territoriesWarmed">The territories warmed successfully.</param>
    /// <param name="firstYear">The inclusive first civil year of the warmed span.</param>
    /// <param name="lastYear">The inclusive last civil year of the warmed span.</param>
    [LoggerMessage(EventId = 4623, Level = LogLevel.Information, Message = "Notable-date cache warm-up completed: {territoriesWarmed} territory(ies) warmed over years {firstYear}..{lastYear}")]
    public static partial void WarmupCompleted(ILogger logger, int territoriesWarmed, int firstYear, int lastYear);

    /// <summary>
    /// Logs that the startup warm-up faulted and was abandoned; the host keeps running and the cache warms lazily.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="exception">The abandoned warm-up fault.</param>
    [LoggerMessage(EventId = 4624, Level = LogLevel.Warning, Message = "Notable-date cache warm-up failed and was abandoned; the cache will warm lazily on demand")]
    public static partial void WarmupFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the registered <see cref="INotableDateService" /> is not the caching decorator, so there is no cache
    /// to warm and the warm-up no-ops.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    [LoggerMessage(EventId = 4625, Level = LogLevel.Warning, Message = "Notable-date cache warm-up skipped: the registered INotableDateService is not the caching decorator; register AddCachedNotableDateService before AddNotableDateCacheWarmup")]
    public static partial void WarmupServiceNotCaching(ILogger logger);
}
