// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series discovered in a Bank of England response: the pair it represents and the IADB metadata
/// that identifies it.
/// </summary>
/// <remarks>
/// Exposed through <see cref="BoeExchangeRateProvider.GetAvailablePairs" /> so callers can discover which currency
/// pairs the loaded BoE data supports without hard-coding the list.
/// </remarks>
public sealed class BoeSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    /// <param name="seriesCode">The IADB series code.</param>
    /// <param name="description">The series description.</param>
    internal BoeSeriesInfo(ExchangeRatePair pair, string quoteIsoCode, string seriesCode, string description)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
        SeriesCode = seriesCode;
        Description = description;
    }

    /// <summary>
    /// Gets the currency pair the series represents, always quoted against the pound sterling.
    /// </summary>
    /// <returns>The <see cref="ExchangeRatePair" /> from <c>GBP</c> to the quote currency.</returns>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <returns>The three-letter ISO code of the quote currency.</returns>
    public string QuoteIsoCode { get; }

    /// <summary>
    /// Gets the IADB series code.
    /// </summary>
    /// <returns>The series code (for example, <c>XUDLUSS</c>).</returns>
    public string SeriesCode { get; }

    /// <summary>
    /// Gets the series description.
    /// </summary>
    /// <returns>The series description text.</returns>
    public string Description { get; }
}
