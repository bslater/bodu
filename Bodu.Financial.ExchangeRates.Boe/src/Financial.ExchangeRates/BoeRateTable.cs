// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing a Bank of England IADB response: the dated daily spot observations, each
/// quoting the pound against a single currency. This is the common shape any BoE source (CSV or otherwise) produces
/// before it is mapped to <see cref="ExchangeRate" /> values.
/// </summary>
internal sealed class BoeRateTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeRateTable" /> class.
    /// </summary>
    /// <param name="observations">The parsed daily spot observations.</param>
    /// <param name="series">The series metadata for the currencies present, keyed by quote ISO code.</param>
    internal BoeRateTable(IReadOnlyList<BoeRateObservation> observations, IReadOnlyDictionary<string, BoeSeries> series)
    {
        Observations = observations;
        Series = series;
    }

    /// <summary>
    /// Gets the parsed daily spot observations.
    /// </summary>
    /// <value>A read-only list of dated, per-currency observations.</value>
    public IReadOnlyList<BoeRateObservation> Observations { get; }

    /// <summary>
    /// Gets the series metadata for the currencies present, keyed by quote ISO code.
    /// </summary>
    /// <value>A read-only map from quote ISO code to its series descriptor.</value>
    public IReadOnlyDictionary<string, BoeSeries> Series { get; }

    /// <summary>
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the pound against a currency on a
    /// given date.
    /// </summary>
    /// <returns>
    /// One <see cref="ExchangeRate" /> per observation whose currency the ISO 4217 catalog lists; an observation of a
    /// configured series whose quote code it does not list is skipped.
    /// </returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (BoeRateObservation observation in Observations)
        {
            if (!CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
                continue;

            yield return new ExchangeRate(
                BoeRateProvider.BaseCurrency,
                quote,
                observation.Date,
                observation.Rate,
                BoeRateProvider.ProviderName);
        }
    }

    /// <summary>
    /// Returns the latest date the table holds an observation for.
    /// </summary>
    /// <returns>The latest observation date, or <see langword="null" /> when the table is empty.</returns>
    public DateOnly? GetLastDate()
    {
        DateOnly? last = null;
        foreach (BoeRateObservation observation in Observations)
        {
            if (last is null || observation.Date > last.Value)
                last = observation.Date;
        }

        return last;
    }

    /// <summary>
    /// Produces the discovered-pair metadata for the distinct currencies in the table.
    /// </summary>
    /// <returns>
    /// One <see cref="BoeSeriesInfo" /> per distinct quote currency the ISO 4217 catalog lists, in first-seen order.
    /// </returns>
    public IReadOnlyList<BoeSeriesInfo> GetSeriesInfo()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<BoeSeriesInfo>();

        foreach (BoeRateObservation observation in Observations)
        {
            if (!seen.Add(observation.CurrencyCode) || !CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
                continue;

            CurrencyPair pair = new(BoeRateProvider.BaseCurrency, quote);
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
