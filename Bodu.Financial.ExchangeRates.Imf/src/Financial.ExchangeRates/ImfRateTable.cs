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
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the US dollar against a currency
    /// on a given date.
    /// </summary>
    /// <returns>One <see cref="ExchangeRate" /> per observation.</returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (ImfRateObservation observation in Observations)
        {
            yield return new ExchangeRate(
                ImfRateProvider.BaseCurrency,
                CurrencyInfo.ParseCurrencyCode(observation.CurrencyCode),
                observation.Date,
                observation.Rate,
                ImfRateProvider.ProviderName);
        }
    }

    /// <summary>
    /// Produces the discovered-pair metadata for the distinct currencies in the table.
    /// </summary>
    /// <returns>One <see cref="ImfSeriesInfo" /> per distinct quote currency, in first-seen order.</returns>
    public IReadOnlyList<ImfSeriesInfo> GetSeriesInfo()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ImfSeriesInfo>();

        foreach (ImfRateObservation observation in Observations)
        {
            if (seen.Add(observation.CurrencyCode))
            {
                CurrencyPair pair = new(ImfRateProvider.BaseCurrency, CurrencyInfo.ParseCurrencyCode(observation.CurrencyCode));
                result.Add(new ImfSeriesInfo(pair, observation.CurrencyCode));
            }
        }

        return result;
    }
}
