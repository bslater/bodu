// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiKeyedOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Samples.JsonSerialization.Scenarios;

/// <summary>
/// Demonstrates the dependency-injection registration surface:
/// <see cref="FinancialJsonServiceCollectionExtensions.AddFinancialJson" /> registers a fully-configured
/// <see cref="JsonSerializerOptions" /> as a keyed singleton, and consumers resolve it with the
/// <see cref="FinancialJsonServiceCollectionExtensions.JsonOptionsKey" /> key (the string <c>"Financial"</c>)
/// rather than constructing and configuring options at each call site.
/// </summary>
public static class DiKeyedOptions
{
    /// <summary>
    /// Registers the keyed options with a chosen policy, then resolves and uses them from the provider.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- DI keyed JsonSerializerOptions (key \"Financial\") ---");

        var services = new ServiceCollection();

        // Register the financial JSON options once, keyed by the "Financial" key, under the Compact policy.
        // This is the companion package's DI entry point; it does not require the core AddFinancialService.
        services.AddFinancialJson(FinancialJsonPolicy.Compact);

        using ServiceProvider provider = services.BuildServiceProvider();

        // Resolve the pre-configured options by key - the constant equals the literal string "Financial".
        var options = provider.GetRequiredKeyedService<JsonSerializerOptions>(
            FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
        Console.WriteLine($"Resolved key : \"{FinancialJsonServiceCollectionExtensions.JsonOptionsKey}\"");

        // Every consumer that resolves the key serializes with the same Compact policy - money as a string.
        Money<USD> price = Money.Of<USD>(19.99m);
        MoneyBag bag = MoneyBag.Of(Money.From(19.99m, CurrencyCode.USD), Money.From(12.34m, CurrencyCode.EUR));
        Console.WriteLine($"Money<USD>   : {JsonSerializer.Serialize(price, options)}");
        Console.WriteLine($"MoneyBag     : {JsonSerializer.Serialize(bag, options)}");

        // The keyed singleton hands back the same configured instance on every resolve.
        var again = provider.GetRequiredKeyedService<JsonSerializerOptions>(
            FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
        Console.WriteLine($"Same instance: {ReferenceEquals(options, again)}");

        Console.WriteLine();
    }
}
