// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from OANDA: the pair it represents, the quote-currency code reported for it,
/// and the price basis the rates were drawn from.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which
/// currency pairs the provider has loaded without hard-coding the list.
/// </remarks>
public sealed class OandaSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OandaSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    /// <param name="price">The price basis the series was fetched at.</param>
    internal OandaSeriesInfo(CurrencyPair pair, string quoteIsoCode, string price)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
        Price = price;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from the base currency to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }

    /// <summary>
    /// Gets the price basis the series was fetched at.
    /// </summary>
    /// <value>The OANDA price basis, for example <c>bid</c>, <c>mid</c>, or <c>ask</c>.</value>
    public string Price { get; }
}
