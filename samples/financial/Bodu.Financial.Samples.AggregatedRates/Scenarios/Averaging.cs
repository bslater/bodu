// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Averaging.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.AggregatedRates.Scenarios;

/// <summary>
/// Demonstrates <see cref="AverageStrategy" />: every child that can serve the pair contributes, and
/// the aggregate returns the mean under a synthetic provider label. Use it to smooth small
/// discrepancies between comparable sources — but not for auditable conversions, because the result
/// no longer traces to a single published rate.
/// </summary>
public static class Averaging
{
    /// <summary>
    /// Averages a pair both banks quote and shows the synthetic provenance label.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Averaging across sources ---");

        var bankA = StaticSources.LoadBankA();
        var bankB = StaticSources.LoadBankB();

        var aggregate = new AggregatingRateProvider(
            [
                new NamedDatedRateProvider(StaticSources.BankA, bankA),
                new NamedDatedRateProvider(StaticSources.BankB, bankB),
            ],
            new RateAggregationOptions
            {
                DefaultStrategy = new AverageStrategy(),
            });

        var date = new DateOnly(2024, 2, 14);

        var a = bankA.GetRate("AUD", "USD", date).Rate.Rate;
        var b = bankB.GetRate("AUD", "USD", date).Rate.Rate;
        RateLookupResult averaged = aggregate.GetRate("AUD", "USD", date);

        Console.WriteLine($"BankA fix : {a}");
        Console.WriteLine($"BankB fix : {b}");
        Console.WriteLine($"Averaged  : {averaged.Rate.Rate}  provider label \"{averaged.Rate.Provider}\" (synthetic - not for audit)");

        // Pairs only one bank quotes still resolve - an average of one contribution is that value.
        RateLookupResult jpy = aggregate.GetRate("AUD", "JPY", date);
        Console.WriteLine($"AUD/JPY   : {jpy.Rate.Rate}  (single contributor)");

        Console.WriteLine();
    }
}
