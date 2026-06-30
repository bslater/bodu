// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing a Bank of England IADB response: the dated daily spot observations, each
/// quoting the pound against a single currency. This is the common shape any BoE source (CSV or otherwise) produces
/// before it is mapped to <see cref="ExchangeRate" /> values.
/// </summary>
internal sealed class BoeExchangeRateTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateTable" /> class.
    /// </summary>
    /// <param name="observations">The parsed daily spot observations.</param>
    /// <param name="series">The series metadata for the currencies present, keyed by quote ISO code.</param>
    internal BoeExchangeRateTable(IReadOnlyList<BoeExchangeRateObservation> observations, IReadOnlyDictionary<string, BoeSeries> series)
    {
        Observations = observations;
        Series = series;
    }

    /// <summary>
    /// Gets the parsed daily spot observations.
    /// </summary>
    /// <value>A read-only list of dated, per-currency observations.</value>
    public IReadOnlyList<BoeExchangeRateObservation> Observations { get; }

    /// <summary>
    /// Gets the series metadata for the currencies present, keyed by quote ISO code.
    /// </summary>
    /// <value>A read-only map from quote ISO code to its series descriptor.</value>
    public IReadOnlyDictionary<string, BoeSeries> Series { get; }

    /// <summary>
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the pound against a currency on a
    /// given date.
    /// </summary>
    /// <returns>One <see cref="ExchangeRate" /> per observation.</returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (BoeExchangeRateObservation observation in Observations)
        {
            yield return new ExchangeRate(
                BoeExchangeRateProvider.BaseCurrency,
                CurrencyInfo.ParseCurrencyCode(observation.CurrencyCode),
                observation.Date,
                observation.Rate,
                BoeExchangeRateProvider.ProviderName);
        }
    }

    /// <summary>
    /// Produces the discovered-pair metadata for the distinct currencies in the table.
    /// </summary>
    /// <returns>One <see cref="BoeSeriesInfo" /> per distinct quote currency, in first-seen order.</returns>
    public IReadOnlyList<BoeSeriesInfo> GetSeriesInfo()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BoeSeriesInfo>();

        foreach (BoeExchangeRateObservation observation in Observations)
        {
            if (!seen.Add(observation.CurrencyCode))
                continue;

            ExchangeRatePair pair = new(BoeExchangeRateProvider.BaseCurrency, CurrencyInfo.ParseCurrencyCode(observation.CurrencyCode));
            Series.TryGetValue(observation.CurrencyCode, out BoeSeries? series);
            result.Add(new BoeSeriesInfo(
                pair,
                observation.CurrencyCode,
                series?.SeriesCode ?? string.Empty,
                series?.Description ?? string.Empty));
        }

        return result;
    }
}
