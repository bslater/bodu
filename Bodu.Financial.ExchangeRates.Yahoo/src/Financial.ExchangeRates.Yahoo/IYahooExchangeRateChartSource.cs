// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IYahooExchangeRateChartSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Obtains a parsed Yahoo Finance chart for a currency pair and date range. This is the seam between the provider and
/// the network: the default implementation downloads and parses the JSON chart response, while tests substitute a
/// fixture-backed implementation.
/// </summary>
internal interface IYahooExchangeRateChartSource
{
    /// <summary>
    /// Fetches and parses the chart for the supplied request.
    /// </summary>
    /// <param name="request">The chart request describing the pair, ticker, and date range.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the fetch.</param>
    /// <returns>A task that yields the parsed chart.</returns>
    ValueTask<YahooExchangeRateChart> GetChartAsync(YahooChartRequest request, CancellationToken cancellationToken = default);
}
