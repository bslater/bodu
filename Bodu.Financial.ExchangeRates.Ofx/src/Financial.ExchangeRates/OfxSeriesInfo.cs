// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from OFX: the pair it represents and the quote-currency code reported for it.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which
/// currency pairs the provider has loaded without hard-coding the list.
/// </remarks>
public sealed class OfxSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfxSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    internal OfxSeriesInfo(CurrencyPair pair, string quoteIsoCode)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
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
}
