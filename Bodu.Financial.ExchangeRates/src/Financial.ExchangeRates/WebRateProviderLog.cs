// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderLog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by
/// <see cref="PairWebRateProvider{TSeries}" /> while fetching and serving exchange rates.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class WebRateProviderLog
{
    /// <summary>
    /// Logs that a pair download is starting.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="pair">A human-readable label for the pair being downloaded.</param>
    [LoggerMessage(EventId = 4401, Message = "Downloading exchange-rate pair '{pair}'")]
    public static partial void PairLoadStarting(ILogger logger, LogLevel level, string pair);

    /// <summary>
    /// Logs that a pair was downloaded and its observations were accumulated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="pair">A human-readable label for the pair that was downloaded.</param>
    /// <param name="rateCount">The number of rate observations accumulated.</param>
    [LoggerMessage(EventId = 4402, Message = "Loaded exchange-rate pair '{pair}' with {rateCount} rate observation(s)")]
    public static partial void PairLoaded(ILogger logger, LogLevel level, string pair, int rateCount);

    /// <summary>
    /// Logs an individual rate observation ingested from a download, for fine-grained diagnostics.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The observed rate.</param>
    [LoggerMessage(EventId = 4405, Message = "Ingested exchange-rate observation {fromIsoCode}->{toIsoCode} on {date} = {rate}")]
    public static partial void ObservationIngested(ILogger logger, LogLevel level, string fromIsoCode, string toIsoCode, DateOnly date, decimal rate);

    /// <summary>
    /// Logs that a pair download failed.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="pair">A human-readable label for the pair whose download failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(EventId = 4403, Message = "Failed to download exchange-rate pair '{pair}'")]
    public static partial void PairLoadFailed(ILogger logger, LogLevel level, string pair, Exception exception);

    /// <summary>
    /// Logs that a synchronous lookup triggered an on-demand network fetch.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="date">The date around which a window was fetched synchronously.</param>
    [LoggerMessage(EventId = 4404, Message = "Performed synchronous network fetch to resolve a rate for {date}")]
    public static partial void SynchronousNetworkFetch(ILogger logger, LogLevel level, DateOnly date);

    /// <summary>
    /// Logs that a pair download threw an exception type the fetch is not expected to produce, indicating a probable
    /// bug rather than a transport or data failure.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="pair">A human-readable label for the pair whose download failed.</param>
    /// <param name="exception">The unexpected exception.</param>
    [LoggerMessage(EventId = 4406, Level = LogLevel.Error, Message = "Unexpected error downloading exchange-rate pair '{pair}'")]
    public static partial void PairLoadUnexpectedError(ILogger logger, string pair, Exception exception);
}
