// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StaticRates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.OfflineRates;

/// <summary>
/// Loads the committed static rate file into a ready-to-query provider. This is the offline-first pattern:
/// any rate data you already hold (a file, a database table, an API response you archived) can be poured
/// through <see cref="RateTableBuilder" /> into an immutable <see cref="RateBook" /> and served through
/// <see cref="FixedDatedRateProvider" /> — the same contracts the live web providers implement.
/// </summary>
public static class StaticRates
{
    /// <summary>
    /// The provider label stamped onto every observation loaded from the sample data file. Lookup results
    /// carry this label in their provenance, so converted amounts stay auditable back to their source.
    /// </summary>
    public const string ProviderName = "SampleData";

    /// <summary>
    /// Reads <c>Data/aud-daily-2024H1.csv</c> and materializes it as a dated rate provider.
    /// </summary>
    /// <returns>An immutable, in-memory <see cref="FixedDatedRateProvider" /> over the file's observations.</returns>
    public static FixedDatedRateProvider LoadAudDaily()
    {
        // RateTableBuilder is the mutable accumulator: one call per observation, any number of
        // (pair, provider) series at once. It normalizes and orders the observations for us.
        var builder = new RateTableBuilder();

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "aud-daily-2024H1.csv");
        foreach (var line in File.ReadLines(path))
        {
            // Skip blank lines, '#' comments, and the column-header row.
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("Date,", StringComparison.Ordinal))
                continue;

            var fields = line.Split(',');
            var pair = new CurrencyPair(
                CurrencyInfo.ParseCurrencyCode(fields[1]),
                CurrencyInfo.ParseCurrencyCode(fields[2]));

            builder.Upsert(
                pair,
                ProviderName,
                DateOnly.Parse(fields[0], CultureInfo.InvariantCulture),
                decimal.Parse(fields[3], CultureInfo.InvariantCulture));
        }

        // ToBook() freezes the accumulated series into an immutable RateBook; wrapping it in a
        // FixedDatedRateProvider adds the full dated-lookup surface (GetRate/TryGetRate/GetRates,
        // date-resolution options, inverse fallback) with no further I/O.
        return new FixedDatedRateProvider(builder.ToBook());
    }
}
