// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="RbaExchangeRateProvider" />
/// while downloading and serving RBA historical exchange rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that an era download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="era">The label of the era being downloaded.</param>
    [LoggerMessage(EventId = 4301, Message = "Downloading RBA era '{era}'")]
    public static partial void EraLoadStarting(ILogger logger, LogLevel level, string era);

    /// <summary>
    /// Logs that an era was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="era">The label of the loaded era.</param>
    /// <param name="rateCount">The number of rate observations accumulated from the era.</param>
    [LoggerMessage(EventId = 4302, Message = "Loaded RBA era '{era}' with {rateCount} rate observation(s)")]
    public static partial void EraLoaded(ILogger logger, LogLevel level, string era, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from an era, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4305, Message = "Ingested RBA observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, LogLevel level, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that an era download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="era">The label of the era whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4303, Message = "Failed to download RBA era '{era}'")]
    public static partial void EraLoadFailed(ILogger logger, LogLevel level, string era, Exception exception);
}
