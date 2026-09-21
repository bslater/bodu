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
        SampleConsole.Scenario(
            "Averaging across sources",
            what: "Asks the same aggregate under the averaging strategy, where several sources answer, and shows "
                + "the combined rate against the individual ones.",
            why: "Priority order says one feed is authoritative; averaging says none of them is, which is the "
                + "right model when the sources are independent quotes rather than a hierarchy. It also dampens "
                + "the single-feed outlier - a stale or mistaken quote moves an average far less than it moves a "
                + "first-success answer. The trade is that an average is not a rate anyone published, so it is "
                + "appropriate for valuation and indicative pricing and not for settling a trade at a quoted "
                + "price.",
            expect: "The aggregate sits between the individual quotes rather than equalling any of them, which "
                + "is both the value and the caveat of the strategy.");

        var bankA = StaticSources.LoadBankA();
        var bankB = StaticSources.LoadBankB();

        // Same two children as the priority scenario, but DefaultStrategy swaps first-success-wins for
        // the mean of every contributor's quote - the strategy applies to any pair without its own route.
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

        Console.WriteLine($"  BankA fix : {a}");
        Console.WriteLine($"  BankB fix : {b}");
        Console.WriteLine($"  Averaged  : {averaged.Rate.Rate}  provider label \"{averaged.Rate.Provider}\" (synthetic - not for audit)");

        // Pairs only one bank quotes still resolve - an average of one contribution is that value.
        RateLookupResult jpy = aggregate.GetRate("AUD", "JPY", date);
        Console.WriteLine($"  AUD/JPY   : {jpy.Rate.Rate}  (single contributor)");

        Console.WriteLine();
    }
}
