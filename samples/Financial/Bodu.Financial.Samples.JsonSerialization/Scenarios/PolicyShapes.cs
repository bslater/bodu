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
        Console.WriteLine("--- Policy shapes: Strict vs Lenient vs Compact ---");

        // One set of fixed values, serialized three ways.
        Money<USD> price = Money.Of<USD>(19.99m);
        MoneyBag bag = MoneyBag.Of(Money.From(19.99m, CurrencyCode.USD), Money.From(12.34m, CurrencyCode.EUR));
        var rate = new ExchangeRate(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 3, 15), 148.25m, "SampleFeed");

        // Strict (default): the canonical object shape for ledgers, persistence, and audit data.
        var strict = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Strict);
        Console.WriteLine("Strict :");
        Console.WriteLine($"  Money<USD>   {JsonSerializer.Serialize(price, strict)}");
        Console.WriteLine($"  MoneyBag     {JsonSerializer.Serialize(bag, strict)}");
        Console.WriteLine($"  ExchangeRate {JsonSerializer.Serialize(rate, strict)}");

        // Compact: money as a single "amount ISO" string, the bag as a flat { "ISO": amount } map, and the
        // rate collapsed to a "pair" property - for APIs and logs where the object shape is too heavy.
        var compact = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Compact);
        Console.WriteLine("Compact :");
        Console.WriteLine($"  Money<USD>   {JsonSerializer.Serialize(price, compact)}");
        Console.WriteLine($"  MoneyBag     {JsonSerializer.Serialize(bag, compact)}");
        Console.WriteLine($"  ExchangeRate {JsonSerializer.Serialize(rate, compact)}");

        // Lenient: identical on-the-wire shape to Strict, but reads trim whitespace and upcase lowercase ISO
        // codes before validation - for ingesting external feeds. It is not a canonical storage shape.
        var lenient = new JsonSerializerOptions().AddFinancialJsonConverters(FinancialJsonPolicy.Lenient);
        Money imported = JsonSerializer.Deserialize<Money>("""{"amount":12.34,"currency":"  usd  "}""", lenient);
        Console.WriteLine("Lenient :");
        Console.WriteLine($"  read {{\"currency\":\"  usd  \"}} -> {imported}");

        Console.WriteLine();
    }
}
