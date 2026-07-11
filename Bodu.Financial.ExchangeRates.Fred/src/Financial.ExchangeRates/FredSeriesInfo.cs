// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FredSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from FRED: the pair it represents and the FRED series identifier the
/// observations were read from.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which currency
/// pairs the provider has loaded, and which FRED series backed each, without hard-coding the list.
/// </remarks>
public sealed class FredSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FredSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="seriesId">The FRED series identifier the observations were read from.</param>
    internal FredSeriesInfo(CurrencyPair pair, string seriesId)
    {
        Pair = pair;
        SeriesId = seriesId;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from the source currency to the destination currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the FRED series identifier the observations were read from.
    /// </summary>
    /// <value>The FRED series id, for example <c>DEXUSEU</c>; empty when the pair maps to no FRED series.</value>
    public string SeriesId { get; }
}
