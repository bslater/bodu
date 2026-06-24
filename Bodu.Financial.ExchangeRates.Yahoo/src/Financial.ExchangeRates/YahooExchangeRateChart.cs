// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateChart.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing a Yahoo Finance chart response: the resolved pair, its ticker, and the
/// dated close observations. This is the shape the source produces before it is mapped to <see cref="ExchangeRate" />
/// values.
/// </summary>
internal sealed class YahooExchangeRateChart
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateChart" /> class.
    /// </summary>
    /// <param name="pair">The resolved currency pair.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code reported for the chart.</param>
    /// <param name="observations">The dated close observations, in ascending date order.</param>
    internal YahooExchangeRateChart(
        ExchangeRatePair pair,
        string symbol,
        string quoteIsoCode,
        IReadOnlyList<ExchangeRateObservation> observations)
    {
        Pair = pair;
        Symbol = symbol;
        QuoteIsoCode = quoteIsoCode;
        Observations = observations;
    }

    /// <summary>
    /// Gets the resolved currency pair.
    /// </summary>
    /// <value>The requested <see cref="ExchangeRatePair" />.</value>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the Yahoo Finance ticker symbol the chart was fetched under.
    /// </summary>
    /// <value>The ticker symbol, for example <c>AUDUSD=X</c>.</value>
    public string Symbol { get; }

    /// <summary>
    /// Gets the quote-currency ISO code reported for the chart.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }

    /// <summary>
    /// Gets the dated close observations.
    /// </summary>
    /// <value>A read-only list of observations, in ascending date order.</value>
    public IReadOnlyList<ExchangeRateObservation> Observations { get; }

    /// <summary>
    /// Produces the discovered-pair metadata for the chart.
    /// </summary>
    /// <returns>The <see cref="YahooSeriesInfo" /> describing this chart's series.</returns>
    public YahooSeriesInfo GetSeriesInfo() => new(Pair, Symbol, QuoteIsoCode);
}
