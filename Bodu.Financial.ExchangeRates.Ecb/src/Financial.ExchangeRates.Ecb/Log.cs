// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="EcbExchangeRateProvider" />
/// while downloading and serving ECB euro reference rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a feed download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="feed">The name of the feed being downloaded.</param>
    [LoggerMessage(EventId = 4101, Message = "Downloading ECB feed '{feed}'")]
    public static partial void FeedLoadStarting(ILogger logger, LogLevel level, string feed);

    /// <summary>
    /// Logs that a feed was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="feed">The name of the loaded feed.</param>
    /// <param name="rateCount">The number of rate observations accumulated from the feed.</param>
    [LoggerMessage(EventId = 4102, Message = "Loaded ECB feed '{feed}' with {rateCount} rate observation(s)")]
    public static partial void FeedLoaded(ILogger logger, LogLevel level, string feed, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from a feed, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4105, Message = "Ingested ECB observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, LogLevel level, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that a feed download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="feed">The name of the feed whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4103, Message = "Failed to download ECB feed '{feed}'")]
    public static partial void FeedLoadFailed(ILogger logger, LogLevel level, string feed, Exception exception);

    /// <summary>
    /// Logs that a synchronous lookup triggered an on-demand network fetch.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="date">The date whose covering feed was fetched synchronously.</param>
    [LoggerMessage(EventId = 4104, Message = "Performed synchronous ECB network fetch to resolve a rate for {date}")]
    public static partial void SynchronousNetworkFetch(ILogger logger, LogLevel level, DateOnly date);
}
