// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series discovered in an RBA workbook: the pair it represents and the RBA metadata that
/// identifies it.
/// </summary>
/// <remarks>
/// Exposed through <see cref="RbaExchangeRateProvider.GetAvailablePairs" /> so callers can discover which currency
/// pairs the loaded RBA data supports without hard-coding the list.
/// </remarks>
public sealed class RbaSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    /// <param name="seriesId">The RBA series identifier (for example, <c>FXRUSD</c>).</param>
    /// <param name="description">The RBA description of the series.</param>
    /// <param name="units">The RBA units label of the series.</param>
    internal RbaSeriesInfo(CurrencyPair pair, string quoteIsoCode, string seriesId, string description, string units)
    {
        Pair = pair;
        QuoteIsoCode = quoteIsoCode;
        SeriesId = seriesId;
        Description = description;
        Units = units;
    }

    /// <summary>
    /// Gets the currency pair the series represents, always quoted against the Australian dollar.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from <c>AUD</c> to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <value>The three-letter ISO code of the quote currency.</value>
    public string QuoteIsoCode { get; }

    /// <summary>
    /// Gets the RBA series identifier.
    /// </summary>
    /// <value>
    /// The RBA series mnemonic (for example, <c>FXRUSD</c>), or an empty string when the workbook omits it.
    /// </value>
    public string SeriesId { get; }

    /// <summary>
    /// Gets the RBA description of the series.
    /// </summary>
    /// <value>The RBA description text, or an empty string when the workbook omits it.</value>
    public string Description { get; }

    /// <summary>
    /// Gets the RBA units label of the series.
    /// </summary>
    /// <value>The RBA units text (typically the quote ISO code).</value>
    public string Units { get; }
}
