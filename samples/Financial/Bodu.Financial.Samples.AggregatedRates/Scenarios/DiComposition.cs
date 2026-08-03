// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiComposition.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Samples.AggregatedRates.Scenarios;

/// <summary>
/// Demonstrates the DI composition: <c>AddFinancialService().AddAggregatedRateProvider(...)</c> builds
/// the whole stack — each child wrapped in a read-through cache, grouped behind one
/// <see cref="IDatedRateProvider" /> registration, with per-pair routes declared fluently. Children
/// are also registered as keyed services, so a specific source stays resolvable by name.
/// </summary>
public static class DiComposition
{
    /// <summary>
    /// Composes the aggregate in a service collection and resolves it back out.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- DI: AddAggregatedRateProvider builder ---");

        var services = new ServiceCollection();

        // The offline feeds are supplied through the factory-based child form. With live providers
        // registered via Add<Source>ExchangeRates(), use AddCachedChild<TProvider>("NAME") instead
        // (see the commented block in Program.cs). Each child gets its own read-through cache - the
        // in-memory factory here; omit the factory to use the registration's default file cache.
        services.AddFinancialService()
            .AddAggregatedRateProvider(agg => agg
                .AddCachedChild(StaticSources.BankA, _ => StaticSources.LoadBankA(), (_, name) => new InMemoryRateCache(name))
                .AddCachedChild(StaticSources.BankB, _ => StaticSources.LoadBankB(), (_, name) => new InMemoryRateCache(name))
                // Unrouted pairs consult the children in registration order; the first success wins.
                .UseDefaultStrategy(PriorityFallbackStrategy.Instance)
                // Per-pair route: AUD/USD prefers Bank B, overriding the default child order for that pair only.
                .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), StaticSources.BankB, StaticSources.BankA)
                // A route can also carry its own strategy: AUD/EUR averages over Bank A's contribution alone.
                .MapPair(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.EUR), new AverageStrategy(), StaticSources.BankA));

        using ServiceProvider provider = services.BuildServiceProvider();

        // Consumers depend on IDatedRateProvider and receive the cached, routed aggregate.
        var rates = provider.GetRequiredService<IDatedRateProvider>();
        var date = new DateOnly(2024, 2, 14);

        RateLookupResult usd = rates.GetRate("AUD", "USD", date);
        Console.WriteLine($"AUD/USD via aggregate : {usd.Rate.Rate}  served by {usd.Rate.Provider}");

        // A specific child (with its cache) remains addressable as a keyed service.
        var bankAOnly = provider.GetRequiredKeyedService<IDatedRateProvider>(StaticSources.BankA);
        RateLookupResult direct = bankAOnly.GetRate("AUD", "USD", date);
        Console.WriteLine($"AUD/USD via \"BankA\"   : {direct.Rate.Rate}  served by {direct.Rate.Provider} (keyed child)");

        Console.WriteLine();
    }
}
