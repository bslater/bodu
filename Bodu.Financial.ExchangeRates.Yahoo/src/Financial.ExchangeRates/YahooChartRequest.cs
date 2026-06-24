// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooChartRequest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes a single Yahoo Finance chart request: the currency pair, its resolved ticker, and the inclusive date range
/// to fetch.
/// </summary>
internal sealed class YahooChartRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YahooChartRequest" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the request resolves.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol to fetch.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    internal YahooChartRequest(ExchangeRatePair pair, string symbol, DateOnly startDate, DateOnly endDate)
    {
        Pair = pair;
        Symbol = symbol;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Gets the currency pair the request resolves.
    /// </summary>
    /// <value>The requested <see cref="ExchangeRatePair" />.</value>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the Yahoo Finance ticker symbol to fetch.
    /// </summary>
    /// <value>The ticker symbol, for example <c>AUDUSD=X</c>.</value>
    public string Symbol { get; }

    /// <summary>
    /// Gets the inclusive start of the range to fetch.
    /// </summary>
    /// <value>The start date.</value>
    public DateOnly StartDate { get; }

    /// <summary>
    /// Gets the inclusive end of the range to fetch.
    /// </summary>
    /// <value>The end date.</value>
    public DateOnly EndDate { get; }
}
