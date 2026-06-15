// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.Aggregation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <content> Source-generated logging messages emitted by <see cref="AggregatingExchangeRateProvider" /> while routing
/// and combining candidate providers. </content>
internal static partial class Log
{
    /// <summary>
    /// Logs the ordered providers a pair was routed to.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="providerOrder">The comma-separated ordered provider names.</param>
    [LoggerMessage(EventId = 4510, Message = "Routing {fromIsoCode}->{toIsoCode} to providers [{providerOrder}]")]
    public static partial void RouteSelected(ILogger logger, LogLevel level, string fromIsoCode, string toIsoCode, string providerOrder);

    /// <summary>
    /// Logs that a single-date lookup was aggregated from the routed candidates.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="strategy">The strategy that produced the result.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    /// <param name="candidateCount">The number of routed candidates.</param>
    [LoggerMessage(EventId = 4511, Message = "Aggregated {fromIsoCode}->{toIsoCode} on {date} via '{strategy}' from {candidateCount} candidate(s)")]
    public static partial void Aggregated(ILogger logger, LogLevel level, string strategy, string fromIsoCode, string toIsoCode, DateOnly date, int candidateCount);

    /// <summary>
    /// Logs that no routed candidate could satisfy a single-date lookup.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="level">The level at which to log the message.</param>
    /// <param name="strategy">The strategy that was applied.</param>
    /// <param name="fromIsoCode">The source-currency ISO code.</param>
    /// <param name="toIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The requested date.</param>
    [LoggerMessage(EventId = 4512, Message = "No candidate satisfied {fromIsoCode}->{toIsoCode} on {date} via '{strategy}'")]
    public static partial void AggregationUnresolved(ILogger logger, LogLevel level, string strategy, string fromIsoCode, string toIsoCode, DateOnly date);
}
