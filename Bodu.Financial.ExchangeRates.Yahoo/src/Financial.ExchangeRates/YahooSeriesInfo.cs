// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from Yahoo Finance: the pair it represents and the ticker that identifies it.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which currency
/// pairs the provider has loaded without hard-coding the list.
/// </remarks>
public sealed class YahooSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YahooSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="symbol">The Yahoo Finance ticker symbol.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    internal YahooSeriesInfo(CurrencyPair pair, string symbol, string quoteIsoCode)
    {
        Pair = pair;
        Symbol = symbol;
        QuoteIsoCode = quoteIsoCode;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from the base currency to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the Yahoo Finance ticker symbol the series was fetched under.
    /// </summary>
    /// <value>The ticker symbol, for example <c>AUDUSD=X</c>.</value>
    public string Symbol { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }
}
