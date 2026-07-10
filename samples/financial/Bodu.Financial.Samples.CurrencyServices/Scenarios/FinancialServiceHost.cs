// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceHost.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Samples.CurrencyServices.Scenarios;

/// <summary>
/// Demonstrates the composition-root wiring: <c>AddFinancialService</c> registers the currency lookup
/// and financial options, the builder chain adds JSON policy and an exchange-rate provider, and
/// <c>UseCurrencyResolution</c> promotes the DI lookup to the ambient seam once at startup.
/// </summary>
public static class FinancialServiceHost
{
    /// <summary>
    /// Composes the financial services and consumes each registration.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AddFinancialService host wiring ---");

        var services = new ServiceCollection();

        // One call registers the core services (ICurrencyLookup, FinancialOptions, financial JSON);
        // the returned builder adds the optional pieces. The rate provider here is an offline
        // instance - a live provider package would use its Add<Source>ExchangeRates() instead.
        services.AddFinancialService()
            .AddFinancialJson(FinancialJsonPolicy.Compact)
            .AddDatedExchangeRateProvider(new FixedDatedRateProvider(
            [
                new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 3, 15), 0.6580m, "Config"),
            ]));

        using ServiceProvider provider = services.BuildServiceProvider();

        // Promote the DI-registered lookup to CurrencyResolution.Current so runtime Money resolves
        // through the container's lookup. Call once from the composition root; tests use PushScoped.
        provider.UseCurrencyResolution();

        // ICurrencyLookup: resolve by ISO or ISO-4217 numeric code over the registry.
        var lookup = provider.GetRequiredService<ICurrencyLookup>();
        if (lookup.TryByNumericCode(36, out CurrencyInfo byNumber))
            Console.WriteLine($"Numeric 036   : {byNumber.IsoCode} ({byNumber.EnglishName})");

        // The keyed JsonSerializerOptions carry the configured financial policy (Compact here).
        var json = provider.GetRequiredKeyedService<JsonSerializerOptions>(FinancialServiceBuilderExtensions.JsonOptionsKey);
        Console.WriteLine($"Financial JSON: {JsonSerializer.Serialize(Money.Of<USD>(19.99m), json)}  (Compact policy)");

        // The registered provider serves rate lookups for consumers depending on IDatedRateProvider.
        var rates = provider.GetRequiredService<IDatedRateProvider>();
        RateLookupResult rate = rates.GetRate("AUD", "USD", new DateOnly(2024, 3, 15));
        Console.WriteLine($"AUD/USD       : {rate.Rate.Rate} [{rate.Rate.Provider}]");

        Console.WriteLine();
    }
}
