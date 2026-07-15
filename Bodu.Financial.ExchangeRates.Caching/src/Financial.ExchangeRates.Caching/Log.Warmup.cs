// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.Warmup.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the source-generated logging messages emitted by <see cref="RateCacheWarmupService" /> around the startup
/// cache warm-up.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that the startup warm-up began, with the pair count and the resolved window.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="pairCount">The number of well-formed pairs being warmed.</param>
    /// <param name="startDate">The inclusive first date of the warmed window.</param>
    /// <param name="endDate">The inclusive last date of the warmed window.</param>
    [LoggerMessage(EventId = 4521, Level = LogLevel.Information, Message = "Rate cache warm-up started: {pairCount} pair(s) over {startDate}..{endDate}")]
    public static partial void WarmupStarted(ILogger logger, int pairCount, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Logs that the startup warm-up completed, with the total pairs warmed across all targets.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="pairsWarmed">The total pairs warmed successfully, summed across the targets.</param>
    /// <param name="targets">The number of caching providers warmed.</param>
    [LoggerMessage(EventId = 4522, Level = LogLevel.Information, Message = "Rate cache warm-up completed: {pairsWarmed} pair(s) warmed across {targets} provider(s)")]
    public static partial void WarmupCompleted(ILogger logger, int pairsWarmed, int targets);

    /// <summary>
    /// Logs that the startup warm-up faulted and was abandoned; the host keeps running and the cache warms lazily.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="exception">The abandoned warm-up fault.</param>
    [LoggerMessage(EventId = 4523, Level = LogLevel.Warning, Message = "Rate cache warm-up failed and was abandoned; the cache will warm lazily on demand")]
    public static partial void WarmupFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that a configured keyed provider name resolved to no warmable caching provider and was skipped.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="name">The keyed provider name that did not resolve.</param>
    [LoggerMessage(EventId = 4524, Level = LogLevel.Warning, Message = "Rate cache warm-up skipped provider '{name}': no keyed caching provider is registered under that name")]
    public static partial void WarmupProviderNotFound(ILogger logger, string name);
}
