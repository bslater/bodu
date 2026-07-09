// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series discovered in an ECB feed: the pair it represents and the quote-currency code.
/// </summary>
/// <remarks>
/// Exposed through <see cref="EcbRateProvider.GetAvailablePairs" /> so callers can discover which currency
/// pairs the loaded ECB data supports without hard-coding the list.
/// </remarks>
public sealed class EcbSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    internal EcbSeriesInfo(CurrencyPair pair, string quoteIsoCode)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
    }

    /// <summary>
    /// Gets the currency pair the series represents, always quoted against the euro.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from <c>EUR</c> to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }
}
