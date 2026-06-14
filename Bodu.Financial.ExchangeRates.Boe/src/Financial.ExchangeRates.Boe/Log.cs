// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="BoeExchangeRateProvider" />
/// while downloading and serving Bank of England daily spot rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a range download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="startDate">The inclusive start of the range being downloaded.</param>
    /// <param name="endDate">The inclusive end of the range being downloaded.</param>
    [LoggerMessage(EventId = 4201, Level = LogLevel.Debug, Message = "Downloading BoE range '{startDate}'..'{endDate}'")]
    public static partial void FeedLoadStarting(ILogger logger, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Logs that a range was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="startDate">The inclusive start of the loaded range.</param>
    /// <param name="endDate">The inclusive end of the loaded range.</param>
    /// <param name="rateCount">The number of rate observations accumulated from the range.</param>
    [LoggerMessage(EventId = 4202, Level = LogLevel.Information, Message = "Loaded BoE range '{startDate}'..'{endDate}' with {rateCount} rate observation(s)")]
    public static partial void FeedLoaded(ILogger logger, DateOnly startDate, DateOnly endDate, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from a range, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4205, Level = LogLevel.Trace, Message = "Ingested BoE observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that a range download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="startDate">The inclusive start of the range whose download failed.</param>
    /// <param name="endDate">The inclusive end of the range whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4203, Level = LogLevel.Warning, Message = "Failed to download BoE range '{startDate}'..'{endDate}'")]
    public static partial void FeedLoadFailed(ILogger logger, DateOnly startDate, DateOnly endDate, Exception exception);
}
