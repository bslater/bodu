// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="YahooExchangeRateProvider" />
/// while fetching and serving Yahoo Finance exchange rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a pair download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol being downloaded.</param>
    [LoggerMessage(EventId = 4401, Level = LogLevel.Debug, Message = "Downloading Yahoo Finance chart '{symbol}'")]
    public static partial void PairLoadStarting(ILogger logger, string symbol);

    /// <summary>
    /// Logs that a pair was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol that was downloaded.</param>
    /// <param name="rateCount">The number of rate observations accumulated from the chart.</param>
    [LoggerMessage(EventId = 4402, Level = LogLevel.Information, Message = "Loaded Yahoo Finance chart '{symbol}' with {rateCount} rate observation(s)")]
    public static partial void PairLoaded(ILogger logger, string symbol, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from a chart, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4405, Level = LogLevel.Trace, Message = "Ingested Yahoo Finance observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that a pair download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4403, Level = LogLevel.Warning, Message = "Failed to download Yahoo Finance chart '{symbol}'")]
    public static partial void PairLoadFailed(ILogger logger, string symbol, Exception exception);

    /// <summary>
    /// Logs that a synchronous lookup triggered an on-demand network fetch.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="date">The date around which a window was fetched synchronously.</param>
    [LoggerMessage(EventId = 4404, Level = LogLevel.Warning, Message = "Performed synchronous Yahoo Finance network fetch to resolve a rate for {date}")]
    public static partial void SynchronousNetworkFetch(ILogger logger, DateOnly date);
}
