// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolicyShapes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Serialization.Json;

namespace Bodu.Financial.Samples.JsonSerialization.Scenarios;

/// <summary>
/// Demonstrates how the <see cref="FinancialJsonPolicy" /> argument to
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" /> selects the wire shape:
/// the canonical <c>Strict</c> object form, the forgiving <c>Lenient</c> import form (same shape, normalising
/// reads), and the terse <c>Compact</c> string form. The same in-memory values are serialized under each policy.
/// </summary>
public static class PolicyShapes
{
    /// <summary>
    /// Serializes the same money, bag, and rate under all three policies, then shows a Lenient read.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Policy shapes - Strict, Lenient, and Compact",
            what: "Emits the same values under each policy and reads back input that only some of them accept.",
            why: "The compact form is what a high-traffic API emits and the explicit form is what survives a "
                + "schema change or an ambiguous reader, so a service usually wants to be strict about what it "
                + "accepts and compact about what it returns. Named policies make that asymmetry easy to state "
                + "at a boundary; individual switches make it something each endpoint reinvents. Leniency in "
                + "particular should be opt-in per boundary rather than a global default, because the inputs "
                + "worth being lenient about are the ones you do not control.",
            expect: "The same value serializes visibly differently per policy while meaning the same thing, and "
                + "input the lenient policy accepts is refused by the strict one.");

        // One set of fixed values, serialized three ways.
        Money<USD> price = Money.Of<USD>(19.99m);
        MoneyBag bag = MoneyBag.Of(Money.From(19.99m, CurrencyCode.USD), Money.From(12.34m, CurrencyCode.EUR));
        var rate = new ExchangeRate(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 3, 15), 148.25m, "SampleFeed");

        // Strict (default): the canonical object shape for ledgers, persistence, and audit data.
        var strict = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Strict);
        Console.WriteLine("  Strict :");
        Console.WriteLine($"  Money<USD>   {JsonSerializer.Serialize(price, strict)}");
        Console.WriteLine($"  MoneyBag     {JsonSerializer.Serialize(bag, strict)}");
        Console.WriteLine($"  ExchangeRate {JsonSerializer.Serialize(rate, strict)}");

        // Compact: money as a single "amount ISO" string, the bag as a flat { "ISO": amount } map, and the
        // rate collapsed to a "pair" property - for APIs and logs where the object shape is too heavy.
        var compact = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
        Console.WriteLine("  Compact :");
        Console.WriteLine($"  Money<USD>   {JsonSerializer.Serialize(price, compact)}");
        Console.WriteLine($"  MoneyBag     {JsonSerializer.Serialize(bag, compact)}");
        Console.WriteLine($"  ExchangeRate {JsonSerializer.Serialize(rate, compact)}");

        // Lenient: identical on-the-wire shape to Strict, but reads trim whitespace and upcase lowercase ISO
        // codes before validation - for ingesting external feeds. It is not a canonical storage shape.
        var lenient = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);
        Money imported = JsonSerializer.Deserialize<Money>("""{"amount":12.34,"currency":"  usd  "}""", lenient);
        Console.WriteLine("  Lenient :");
        Console.WriteLine($"  read {{\"currency\":\"  usd  \"}} -> {imported}");

        Console.WriteLine();
    }
}
