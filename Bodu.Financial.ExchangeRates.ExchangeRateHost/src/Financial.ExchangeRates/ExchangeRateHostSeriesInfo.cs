// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from exchangerate.host: the pair it represents and the source and quote
/// currencies the response was denominated in.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which currency
/// pairs the provider has loaded without hard-coding the list.
/// </remarks>
public sealed class ExchangeRateHostSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateHostSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="sourceIsoCode">The source-currency ISO code the response was denominated in.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    internal ExchangeRateHostSeriesInfo(CurrencyPair pair, string sourceIsoCode, string quoteIsoCode)
    {
        Pair = pair;
        SourceIsoCode = sourceIsoCode;
        QuoteIsoCode = quoteIsoCode;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from the source currency to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the source-currency ISO code the exchangerate.host response was denominated in.
    /// </summary>
    /// <value>The three-letter ISO code of the source currency.</value>
    public string SourceIsoCode { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }
}
