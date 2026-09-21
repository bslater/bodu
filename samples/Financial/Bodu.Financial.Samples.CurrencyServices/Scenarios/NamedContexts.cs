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
        SampleConsole.Scenario(
            "Named monetary contexts",
            what: "Registers several named contexts with different rounding and currency settings and resolves "
                + "amounts under each.",
            why: "One process often needs more than one monetary policy: a trading desk and a retail ledger in "
                + "the same service round differently and settle in different currencies, and neither should "
                + "have to know about the other. Naming the contexts keeps each policy in one place and makes "
                + "the choice at the call site a name rather than a bundle of options - which matters because "
                + "options passed individually drift apart, and two code paths that were meant to share a "
                + "rounding rule quietly stop doing so.",
            expect: "The same amount resolves differently under each named context, with the difference coming "
                + "from the named policy rather than from arguments at the call site.");

        // Contexts are records: derive variants from Default with `with` overrides. Retail settles
        // away-from-zero (customer-friendly cash totals); Treasury keeps banker's rounding.
        MonetaryContext retail = MonetaryContext.Default with
        {
            Rounding = MidpointRoundingStrategy.AwayFromZero,
        };
        MonetaryContext treasury = MonetaryContext.Default;

        // The same computed amount settles differently under each policy.
        var computed = new CalculatedMoney(19.985m, CurrencyCode.USD);
        Console.WriteLine($"  Computed value : {computed.Amount} USD (unrounded)");
        Console.WriteLine($"  Retail  settle : {computed.RoundToMoney(retail)}   (away from zero)");
        Console.WriteLine($"  Treasury settle: {computed.RoundToMoney(treasury)}   (banker's rounding)");

        // Register the contexts by name; consumers take [FromKeyedServices("Retail")] MonetaryContext
        // (or resolve keyed, as here) instead of hard-coding policy at each call site.
        var services = new ServiceCollection();
        services.AddFinancialService()
            .AddMonetaryContext("Retail", retail)
            .AddMonetaryContext("Treasury", treasury);

        using ServiceProvider provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredKeyedService<MonetaryContext>("Retail");
        Console.WriteLine($"  Keyed \"Retail\" : {computed.RoundToMoney(resolved)}   (resolved from DI)");

        Console.WriteLine();
    }
}
