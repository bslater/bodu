// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents the normalized result of parsing an RBA workbook: the discovered currency series and the dated rows of
/// rate values. This is the common shape any RBA source (workbook or CSV) produces before it is mapped to
/// <see cref="ExchangeRate" /> values.
/// </summary>
internal sealed class RbaRateTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateTable" /> class.
    /// </summary>
    /// <param name="series">The currency series, in column order.</param>
    /// <param name="rows">
    /// The dated rows of rate values, each positionally aligned to <paramref name="series" />.
    /// </param>
    internal RbaRateTable(IReadOnlyList<RbaRateSeries> series, IReadOnlyList<RbaRateRow> rows)
    {
        Series = series;
        Rows = rows;
    }

    /// <summary>
    /// Gets the discovered currency series, in column order.
    /// </summary>
    /// <value>A read-only list of series descriptors.</value>
    public IReadOnlyList<RbaRateSeries> Series { get; }

    /// <summary>
    /// Gets the dated rows of rate values.
    /// </summary>
    /// <value>A read-only list of rows, each positionally aligned to <see cref="Series" />.</value>
    public IReadOnlyList<RbaRateRow> Rows { get; }

    /// <summary>
    /// Gets or sets the instant the table's bytes were fetched from the source when they were served from a disk cache.
    /// </summary>
    /// <value>
    /// The cache entry's stored time, or <see langword="null" /> when the bytes were just downloaded (the provider then
    /// stamps the load with the current time).
    /// </value>
    public DateTimeOffset? CachedAtUtc { get; set; }

    /// <summary>
    /// Enumerates the table as <see cref="ExchangeRate" /> observations, each quoting the Australian dollar against a
    /// series' currency on a given date.
    /// </summary>
    /// <returns>One <see cref="ExchangeRate" /> per present, strictly positive cell.</returns>
    public IEnumerable<ExchangeRate> EnumerateRates()
    {
        foreach (RbaRateRow row in Rows)
        {
            for (int i = 0; i < Series.Count; i++)
            {
                decimal? value = row.Values[i];

                // Skip series whose code is not a circulating ISO 4217 currency (for example XDR, the IMF Special
                // Drawing Rights unit): the runtime exchange-rate types model currencies only.
                if (value is > 0m && CurrencyInfo.TryGetCurrencyCode(Series[i].CurrencyCode, out CurrencyCode quote))
                {
                    yield return new ExchangeRate(
                        RbaRateProvider.BaseCurrency,
                        quote,
                        row.Date,
                        value.Value,
                        RbaRateProvider.ProviderName);
                }
            }
        }
    }

    /// <summary>
    /// Returns the latest date the table holds a row for.
    /// </summary>
    /// <returns>The latest row date, or <see langword="null" /> when the table has no rows.</returns>
    public DateOnly? GetLastDate()
    {
        DateOnly? last = null;
        foreach (RbaRateRow row in Rows)
        {
            if (last is null || row.Date > last.Value)
                last = row.Date;
        }

        return last;
    }

    /// <summary>
    /// Produces the discovered-pair metadata for the series in the table.
    /// </summary>
    /// <returns>One <see cref="RbaSeriesInfo" /> per currency series.</returns>
    public IReadOnlyList<RbaSeriesInfo> GetSeriesInfo()
    {
        var result = new List<RbaSeriesInfo>(Series.Count);
        foreach (RbaRateSeries series in Series)
        {
            // Skip non-currency series (for example XDR, the IMF Special Drawing Rights unit) that the runtime
            // exchange-rate types cannot represent.
            if (!CurrencyInfo.TryGetCurrencyCode(series.CurrencyCode, out CurrencyCode quote))
                continue;

            CurrencyPair pair = new(RbaRateProvider.BaseCurrency, quote);
            result.Add(new RbaSeriesInfo(pair, series.CurrencyCode, series.SeriesId, series.Description, series.Units));
        }

        return result;
    }
}
