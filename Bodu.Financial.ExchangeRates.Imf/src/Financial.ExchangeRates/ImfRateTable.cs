// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing an IMF Representative Exchange Rates report: the dated observations,
/// each quoting the US dollar against a single currency. This is the shape the report parser produces before it is
/// mapped to <see cref="ExchangeRate" /> values.
/// </summary>
internal sealed class ImfRateTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImfRateTable" /> class.
    /// </summary>
    /// <param name="observations">The parsed USD-anchored observations.</param>
    internal ImfRateTable(IReadOnlyList<ImfRateObservation> observations)
    {
        Observations = observations;
    }

    /// <summary>
    /// Gets the parsed USD-anchored observations.
    /// </summary>
    /// <value>A read-only list of dated, per-currency observations.</value>
    public IReadOnlyList<ImfRateObservation> Observations { get; }

    /// <summary>
    /// Gets or sets the instant the table's bytes were fetched from the source when they were served from a disk cache.
    /// </summary>
    /// <value>
    /// The cache entry's stored time, or <see langword="null" /> when the bytes were just downloaded (the provider then
    /// stamps the load with the current time).
    /// </value>
    public DateTimeOffset? CachedAtUtc { get; set; }

    /// <summary>
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the US dollar against a currency
    /// on a given date.
    /// </summary>
    /// <returns>
    /// One <see cref="ExchangeRate" /> per observation whose currency the ISO 4217 catalog lists; any other observation
    /// is skipped.
    /// </returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (ImfRateObservation observation in Observations)
        {
            if (!CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
                continue;

            yield return new ExchangeRate(
                ImfRateProvider.BaseCurrency,
                quote,
                observation.Date,
                observation.Rate,
                ImfRateProvider.ProviderName);
        }
    }

    /// <summary>
    /// Returns the latest date the table holds an observation for.
    /// </summary>
    /// <returns>The latest observation date, or <see langword="null" /> when the table is empty.</returns>
    public DateOnly? GetLastDate()
    {
        DateOnly? last = null;
        foreach (ImfRateObservation observation in Observations)
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
    /// One <see cref="ImfSeriesInfo" /> per distinct quote currency the ISO 4217 catalog lists, in first-seen order.
    /// </returns>
    public IReadOnlyList<ImfSeriesInfo> GetSeriesInfo()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ImfSeriesInfo>();

        foreach (ImfRateObservation observation in Observations)
        {
            if (seen.Add(observation.CurrencyCode) && CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
            {
                CurrencyPair pair = new(ImfRateProvider.BaseCurrency, quote);
                result.Add(new ImfSeriesInfo(pair, observation.CurrencyCode));
            }
        }

        return result;
    }
}
