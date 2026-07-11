// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PerPairRouting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Financial.Samples.AggregatedRates.Scenarios;

/// <summary>
/// Demonstrates per-pair routing with <see cref="CurrencyPairRoute" />: individual currency pairs get
/// their own provider order — and optionally their own strategy — while everything else follows the
/// aggregator's defaults. Route each pair to its authoritative source.
/// </summary>
public static class PerPairRouting
{
    /// <summary>
    /// Routes AUD/USD to Bank B first and averages AUD/USD's route-less sibling pairs by default.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Per-pair routing ---");

        var options = new RateAggregationOptions();

        // AUD/USD: prefer Bank B for this pair only (say Bank B is the authoritative USD source),
        // overriding the default child order.
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD)] =
            new CurrencyPairRoute([StaticSources.BankB, StaticSources.BankA]);

        // AUD/EUR: route to Bank A with an explicit per-pair strategy (here the default priority
        // fallback, spelled out to show the seam - any IRateAggregationStrategy plugs in).
        options.Routes[new CurrencyPair(CurrencyCode.AUD, CurrencyCode.EUR)] =
            new CurrencyPairRoute([StaticSources.BankA], PriorityFallbackStrategy.Instance);

        var aggregate = new AggregatingRateProvider(
            [
                new NamedDatedRateProvider(StaticSources.BankA, StaticSources.LoadBankA()),
                new NamedDatedRateProvider(StaticSources.BankB, StaticSources.LoadBankB()),
            ],
            options);

        var date = new DateOnly(2024, 2, 14);

        RateLookupResult usd = aggregate.GetRate("AUD", "USD", date);
        Console.WriteLine($"AUD/USD: {usd.Rate.Rate}  served by {usd.Rate.Provider}  (routed to BankB first)");

        RateLookupResult eur = aggregate.GetRate("AUD", "EUR", date);
        Console.WriteLine($"AUD/EUR: {eur.Rate.Rate}  served by {eur.Rate.Provider}  (routed to BankA)");

        // Unrouted pairs keep the default child order - AUD/JPY still falls through to Bank B.
        RateLookupResult jpy = aggregate.GetRate("AUD", "JPY", date);
        Console.WriteLine($"AUD/JPY: {jpy.Rate.Rate}  served by {jpy.Rate.Provider}  (no route, default order)");

        Console.WriteLine();
    }
}
