// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing an ECB feed: the dated euro reference-rate observations, each quoting
/// the euro against a single currency. This is the common shape any ECB source (XML, SDMX, or CSV) produces before it
/// is mapped to <see cref="ExchangeRate" /> values.
/// </summary>
internal sealed class EcbRateTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbRateTable" /> class.
    /// </summary>
    /// <param name="observations">The parsed euro reference-rate observations.</param>
    internal EcbRateTable(IReadOnlyList<EcbRateObservation> observations)
    {
        Observations = observations;
    }

    /// <summary>
    /// Gets the parsed euro reference-rate observations.
    /// </summary>
    /// <value>A read-only list of dated, per-currency observations.</value>
    public IReadOnlyList<EcbRateObservation> Observations { get; }

    /// <summary>
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the euro against a currency on a
    /// given date.
    /// </summary>
    /// <returns>
    /// One <see cref="ExchangeRate" /> per observation whose currency the ISO 4217 catalog lists; an observation of a
    /// currency it does not list (for example <c>TRL</c>, the Turkish lira retired in 2005, which the full-history feed
    /// still quotes) is skipped.
    /// </returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (EcbRateObservation observation in Observations)
        {
            if (!CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
                continue;

            yield return new ExchangeRate(
                EcbRateProvider.BaseCurrency,
                quote,
                observation.Date,
                observation.Rate,
                EcbRateProvider.ProviderName);
        }
    }

    /// <summary>
    /// Returns the latest date the table holds an observation for.
    /// </summary>
    /// <returns>The latest observation date, or <see langword="null" /> when the table is empty.</returns>
    public DateOnly? GetLastDate()
    {
        DateOnly? last = null;
        foreach (EcbRateObservation observation in Observations)
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
    /// One <see cref="EcbSeriesInfo" /> per distinct quote currency the ISO 4217 catalog lists, in first-seen order.
    /// </returns>
    public IReadOnlyList<EcbSeriesInfo> GetSeriesInfo()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<EcbSeriesInfo>();

        foreach (EcbRateObservation observation in Observations)
        {
            if (seen.Add(observation.CurrencyCode) && CurrencyInfo.TryGetCurrencyCode(observation.CurrencyCode, out CurrencyCode quote))
            {
                CurrencyPair pair = new(EcbRateProvider.BaseCurrency, quote);
                result.Add(new EcbSeriesInfo(pair, observation.CurrencyCode));
            }
        }

        return result;
    }
}
