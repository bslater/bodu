// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// Describes one currency series fetched from Yahoo Finance: the pair it represents and the ticker that identifies it.
/// </summary>
/// <remarks>
/// Exposed through <see cref="YahooExchangeRateProvider.GetAvailablePairs" /> so callers can discover which currency
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
    internal YahooSeriesInfo(ExchangeRatePair pair, string symbol, string quoteIsoCode)
    {
        Pair = pair;
        Symbol = symbol;
        QuoteIsoCode = quoteIsoCode;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <returns>The <see cref="ExchangeRatePair" /> from the base currency to the quote currency.</returns>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the Yahoo Finance ticker symbol the series was fetched under.
    /// </summary>
    /// <returns>The ticker symbol, for example <c>AUDUSD=X</c>.</returns>
    public string Symbol { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <returns>The three-letter ISO code of the quote currency.</returns>
    public string QuoteIsoCode { get; }
}
