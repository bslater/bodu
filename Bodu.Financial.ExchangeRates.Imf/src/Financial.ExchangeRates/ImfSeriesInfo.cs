// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one currency series fetched from the IMF SDMX-JSON service: the pair it represents and the SDMX series key
/// that was queried.
/// </summary>
/// <remarks>
/// Exposed through <see cref="PairWebRateProvider{TSeries}.GetAvailablePairs" /> so callers can discover which currency
/// pairs the provider has loaded without hard-coding the list.
/// </remarks>
public sealed class ImfSeriesInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfSeriesInfo" /> class.
    /// </summary>
    /// <param name="pair">The currency pair the series represents.</param>
    /// <param name="seriesKey">The SDMX series key fragment that was queried.</param>
    internal ImfSeriesInfo(CurrencyPair pair, string seriesKey)
    {
        Pair = pair;
        SeriesKey = seriesKey;
    }

    /// <summary>
    /// Gets the currency pair the series represents.
    /// </summary>
    /// <value>The <see cref="CurrencyPair" /> from the base currency to the quote currency.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the SDMX series key fragment queried for this pair.
    /// </summary>
    /// <value>The <c>{freq}.{area}.{indicator}</c> series key, for example <c>D.GB.ENDE_XDC_USD_RATE</c>.</value>
    public string SeriesKey { get; }
}
