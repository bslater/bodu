// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamedContexts.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Samples.CurrencyServices.Scenarios;

/// <summary>
/// Demonstrates <see cref="MonetaryContext" /> — the immutable bundle of rounding, scale, cash, and
/// allocation policy — and registering several of them as named (keyed) services so different parts
/// of an application settle money under different rules.
/// </summary>
public static class NamedContexts
{
    /// <summary>
    /// Builds two contexts with record overrides, registers them by name, and settles under each.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Named monetary contexts ---");

        // Contexts are records: derive variants from Default with `with` overrides. Retail settles
        // away-from-zero (customer-friendly cash totals); Treasury keeps banker's rounding.
        MonetaryContext retail = MonetaryContext.Default with
        {
            Rounding = MidpointRoundingStrategy.AwayFromZero,
        };
        MonetaryContext treasury = MonetaryContext.Default;

        // The same computed amount settles differently under each policy.
        var computed = new CalculatedMoney(19.985m, CurrencyCode.USD);
        Console.WriteLine($"Computed value : {computed.Amount} USD (unrounded)");
        Console.WriteLine($"Retail  settle : {computed.RoundToMoney(retail)}   (away from zero)");
        Console.WriteLine($"Treasury settle: {computed.RoundToMoney(treasury)}   (banker's rounding)");

        // Register the contexts by name; consumers take [FromKeyedServices("Retail")] MonetaryContext
        // (or resolve keyed, as here) instead of hard-coding policy at each call site.
        var services = new ServiceCollection();
        services.AddFinancialService()
            .AddMonetaryContext("Retail", retail)
            .AddMonetaryContext("Treasury", treasury);

        using ServiceProvider provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredKeyedService<MonetaryContext>("Retail");
        Console.WriteLine($"Keyed \"Retail\" : {computed.RoundToMoney(resolved)}   (resolved from DI)");

        Console.WriteLine();
    }
}
