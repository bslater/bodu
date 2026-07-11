// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="ImfRateProvider" /> while
/// downloading and serving IMF Representative Exchange Rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a report download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="month">The report month being downloaded.</param>
    [LoggerMessage(EventId = 4301, Message = "Downloading IMF report '{month}'")]
    public static partial void ReportLoadStarting(ILogger logger, LogLevel level, string month);

    /// <summary>
    /// Logs that a report was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="month">The loaded report month.</param>
    /// <param name="rateCount">The number of rate observations accumulated from the report.</param>
    [LoggerMessage(EventId = 4302, Message = "Loaded IMF report '{month}' with {rateCount} rate observation(s)")]
    public static partial void ReportLoaded(ILogger logger, LogLevel level, string month, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from a report, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4305, Message = "Ingested IMF observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, LogLevel level, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that a report download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="month">The report month whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4303, Message = "Failed to download IMF report '{month}'")]
    public static partial void ReportLoadFailed(ILogger logger, LogLevel level, string month, Exception exception);

    /// <summary>
    /// Logs that a synchronous lookup triggered an on-demand network fetch.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="date">The date whose covering report was fetched synchronously.</param>
    [LoggerMessage(EventId = 4304, Message = "Performed synchronous IMF network fetch to resolve a rate for {date}")]
    public static partial void SynchronousNetworkFetch(ILogger logger, LogLevel level, DateOnly date);

    /// <summary>
    /// Logs that a report download threw an exception type the fetch is not expected to produce, indicating a probable
    /// bug rather than a transport or data failure.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="month">The report month whose download failed.</param>
    /// <param name="exception">The unexpected exception.</param>
    [LoggerMessage(EventId = 4306, Level = LogLevel.Error, Message = "Unexpected error downloading IMF report '{month}'")]
    public static partial void ReportLoadUnexpectedError(ILogger logger, string month, Exception exception);
}
