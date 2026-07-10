// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityFallback.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.AggregatedRates.Scenarios;

/// <summary>
/// Demonstrates <see cref="AggregatingRateProvider" /> with the default
/// <see cref="PriorityFallbackStrategy" />: children are consulted in order and the first success wins,
/// so a pair the primary source does not quote falls through to the next source automatically.
/// </summary>
public static class PriorityFallback
{
    /// <summary>
    /// Groups the two banks and resolves pairs that exercise the fallback.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Priority fallback (first success wins) ---");

        // Children in priority order: Bank A is preferred, Bank B is the fallback. The aggregator
        // implements IDatedRateProvider itself, so consumers see one provider.
        var aggregate = new AggregatingRateProvider(
        [
            new NamedDatedRateProvider(StaticSources.BankA, StaticSources.LoadBankA()),
            new NamedDatedRateProvider(StaticSources.BankB, StaticSources.LoadBankB()),
        ]);

        var date = new DateOnly(2024, 2, 14);

        // Both banks quote AUD/USD - Bank A wins on priority. Provenance names the serving source.
        RateLookupResult usd = aggregate.GetRate("AUD", "USD", date);
        Console.WriteLine($"AUD/USD: {usd.Rate.Rate}  served by {usd.Rate.Provider}  (both quote it; priority wins)");

        // Only Bank A quotes AUD/EUR.
        RateLookupResult eur = aggregate.GetRate("AUD", "EUR", date);
        Console.WriteLine($"AUD/EUR: {eur.Rate.Rate}  served by {eur.Rate.Provider}");

        // Bank A does not quote AUD/JPY, so the lookup falls through to Bank B.
        RateLookupResult jpy = aggregate.GetRate("AUD", "JPY", date);
        Console.WriteLine($"AUD/JPY: {jpy.Rate.Rate}  served by {jpy.Rate.Provider}  (fallback)");

        Console.WriteLine();
    }
}
