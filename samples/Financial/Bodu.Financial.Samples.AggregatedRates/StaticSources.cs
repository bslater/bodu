// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StaticSources.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.AggregatedRates;

/// <summary>
/// Loads the two committed static rate files that stand in for two live central-bank feeds. Their
/// coverage is deliberately complementary — Bank A quotes AUD/USD and AUD/EUR, Bank B quotes AUD/USD
/// (a visibly different fix) and AUD/JPY — so fallback, averaging, and routing all have something to do.
/// </summary>
public static class StaticSources
{
    /// <summary>The provider label for the first offline feed (quotes AUD/USD and AUD/EUR).</summary>
    public const string BankA = "BankA";

    /// <summary>The provider label for the second offline feed (quotes AUD/USD and AUD/JPY).</summary>
    public const string BankB = "BankB";

    /// <summary>
    /// Loads Bank A's observations from <c>Data/central-bank-a.csv</c>.
    /// </summary>
    /// <returns>A dated provider over Bank A's pairs.</returns>
    public static FixedDatedRateProvider LoadBankA() =>
        Load("central-bank-a.csv", BankA);

    /// <summary>
    /// Loads Bank B's observations from <c>Data/central-bank-b.csv</c>.
    /// </summary>
    /// <returns>A dated provider over Bank B's pairs.</returns>
    public static FixedDatedRateProvider LoadBankB() =>
        Load("central-bank-b.csv", BankB);

    /// <summary>
    /// Reads one CSV and materializes it as a provider whose observations carry <paramref name="provider" />.
    /// </summary>
    /// <param name="fileName">The data file name under <c>Data/</c>.</param>
    /// <param name="provider">The provider label stamped onto each observation.</param>
    /// <returns>An immutable, in-memory provider over the file's observations.</returns>
    private static FixedDatedRateProvider Load(string fileName, string provider)
    {
        var builder = new RateTableBuilder();

        foreach (var line in File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Data", fileName)))
        {
            if (line.Length == 0 || line[0] == '#' || line.StartsWith("Date,", StringComparison.Ordinal))
                continue;

            var fields = line.Split(',');
            builder.Upsert(
                new CurrencyPair(CurrencyInfo.ParseCurrencyCode(fields[1]), CurrencyInfo.ParseCurrencyCode(fields[2])),
                provider,
                DateOnly.Parse(fields[0], CultureInfo.InvariantCulture),
                decimal.Parse(fields[3], CultureInfo.InvariantCulture));
        }

        // ToBook() freezes the accumulated observations into an immutable RateBook; the provider wrapper
        // adds the full dated-lookup surface (date resolution, inverse fallback) with no further I/O.
        return new FixedDatedRateProvider(builder.ToBook());
    }
}
