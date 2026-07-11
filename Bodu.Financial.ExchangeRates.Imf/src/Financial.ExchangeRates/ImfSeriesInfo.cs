// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series discovered in an IMF Representative Exchange Rates report: the pair it represents and
/// the quote-currency code.
/// </summary>
/// <remarks>
/// Exposed through <see cref="ImfRateProvider.GetAvailablePairs" /> so callers can discover which currency pairs the
/// loaded IMF data supports without hard-coding the list. Every pair is quoted against the US dollar.
/// </remarks>
public sealed class ImfSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    internal ImfSeriesInfo(CurrencyPair pair, string quoteIsoCode)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
    }

    /// <summary>
    /// Gets the currency pair the series represents, always quoted against the US dollar.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from <c>USD</c> to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }
}
