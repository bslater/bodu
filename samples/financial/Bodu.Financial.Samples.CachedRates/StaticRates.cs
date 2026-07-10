// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StaticRates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.CachedRates;

/// <summary>
/// Loads the committed static rate file into a provider that stands in for a live web source. Every
/// scenario in this sample wraps the result in a caching decorator exactly as it would wrap a real
/// <c>RbaRateProvider</c> or <c>EcbRateProvider</c> — the contracts are identical.
/// </summary>
public static class StaticRates
{
    /// <summary>
    /// The provider label stamped onto every observation. Caches are bound to this label, and lookup
    /// provenance carries it, so cached rows stay attributable to their source.
    /// </summary>
    public const string ProviderName = "SampleData";

    /// <summary>
    /// Reads <c>Data/aud-daily-2024H1.csv</c> and materializes it as a dated rate provider.
    /// </summary>
    /// <returns>An immutable, in-memory <see cref="FixedDatedRateProvider" /> over the file's observations.</returns>
    public static FixedDatedRateProvider LoadAudDaily()
    {
        var builder = new RateTableBuilder();

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "aud-daily-2024H1.csv");
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("Date,", StringComparison.Ordinal))
                continue;

            var fields = line.Split(',');
            builder.Upsert(
                new CurrencyPair(CurrencyInfo.ParseCurrencyCode(fields[1]), CurrencyInfo.ParseCurrencyCode(fields[2])),
                ProviderName,
                DateOnly.Parse(fields[0], CultureInfo.InvariantCulture),
                decimal.Parse(fields[3], CultureInfo.InvariantCulture));
        }

        return new FixedDatedRateProvider(builder.ToBook());
    }
}
